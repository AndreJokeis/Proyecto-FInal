// ============================================================================
// ARCHIVO: Services/ProductService.cs
// PROPÓSITO: Lógica de negocio para el Catálogo de Productos, Categorías y Proveedores.
//            AQUÍ SE HACE:
//            1. ObtenerTodosAsync: Lista productos con su Categoría y Proveedor.
//               Si quien consulta es un Customer, oculta los descontinuados;
//               si es Admin/Employee, puede incluirlos para reactivarlos o editarlos.
//            2. CrearAsync / EditarAsync: Sanitiza textos con .Trim(), valida que no
//               exista otro producto con el mismo nombre (409 Conflict) y verifica
//               que CategoryId y SupplierId existan en MySQL.
//            3. DescontinuarAsync: Realiza la BAJA LÓGICA de productos marcando
//               product.Discontinued = true (sin hacer DELETE físico en MySQL
//               para no romper los detalles de ventas históricas en 'order details').
// ============================================================================

using APIWeb.Data;
using APIWeb.DTOs.Products;
using APIWeb.Models;
using APIWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Services;

public class ProductService : IProductService
{
    private readonly ContextoBD _context;

    public ProductService(ContextoBD context)
    {
        _context = context;
    }

    public async Task<ServiceResult<List<ProductResponseDto>>> ObtenerTodosAsync(bool includeDiscontinued = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        // Si no se solicitan descontinuados (por ejemplo, en el POS o vista de cliente), los filtra
        if (!includeDiscontinued)
            query = query.Where(p => p.Discontinued != true);

        var products = await query
            .OrderBy(p => p.ProductName)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return ServiceResult<List<ProductResponseDto>>.Ok(products);
    }

    public async Task<ServiceResult<ProductResponseDto>> ObtenerPorIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product is null)
            return ServiceResult<ProductResponseDto>.Fail(404, "Producto no encontrado.");

        return ServiceResult<ProductResponseDto>.Ok(MapToDto(product));
    }

    public async Task<ServiceResult<ProductResponseDto>> CrearAsync(CreateProductRequestDto request)
    {
        var productName = request.ProductName.Trim();
        var quantityPerUnit = string.IsNullOrWhiteSpace(request.QuantityPerUnit)
            ? null
            : request.QuantityPerUnit.Trim();

        // REGLA DE NEGOCIO (Evitar duplicados): verifica que no exista otro producto con el mismo nombre
        var duplicateName = await _context.Products
            .AnyAsync(p => p.ProductName.ToLower() == productName.ToLower());

        if (duplicateName)
            return ServiceResult<ProductResponseDto>.Fail(409, "Ya existe un producto registrado con ese nombre.");

        // Verifica que la categoría y el proveedor seleccionados existan en la BD
        var fkValidation = await ValidateForeignKeysAsync(request.CategoryId, request.SupplierId);
        if (fkValidation is not null)
            return ServiceResult<ProductResponseDto>.Fail(400, fkValidation);

        var product = new Product
        {
            ProductName = productName,
            SupplierId = request.SupplierId,
            CategoryId = request.CategoryId,
            QuantityPerUnit = quantityPerUnit,
            UnitPrice = request.UnitPrice,
            UnitsInStock = request.UnitsInStock,
            Discontinued = request.Discontinued
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var created = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstAsync(p => p.ProductId == product.ProductId);

        return ServiceResult<ProductResponseDto>.Ok(MapToDto(created), 201, "Producto registrado correctamente.");
    }

    public async Task<ServiceResult<ProductResponseDto>> EditarAsync(int id, UpdateProductRequestDto request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null)
            return ServiceResult<ProductResponseDto>.Fail(404, "Producto no encontrado.");

        var productName = request.ProductName.Trim();
        var quantityPerUnit = string.IsNullOrWhiteSpace(request.QuantityPerUnit)
            ? null
            : request.QuantityPerUnit.Trim();

        // REGLA DE NEGOCIO (Evitar duplicados al editar): ningún otro producto puede tener ese nombre
        var duplicateName = await _context.Products
            .AnyAsync(p => p.ProductId != id && p.ProductName.ToLower() == productName.ToLower());

        if (duplicateName)
            return ServiceResult<ProductResponseDto>.Fail(409, "Ya existe otro producto registrado con ese nombre.");

        var fkValidation = await ValidateForeignKeysAsync(request.CategoryId, request.SupplierId);
        if (fkValidation is not null)
            return ServiceResult<ProductResponseDto>.Fail(400, fkValidation);

        product.ProductName = productName;
        product.SupplierId = request.SupplierId;
        product.CategoryId = request.CategoryId;
        product.QuantityPerUnit = quantityPerUnit;
        product.UnitPrice = request.UnitPrice;
        product.UnitsInStock = request.UnitsInStock;
        product.Discontinued = request.Discontinued;

        await _context.SaveChangesAsync();

        var updated = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstAsync(p => p.ProductId == id);

        return ServiceResult<ProductResponseDto>.Ok(MapToDto(updated), 200, "Producto actualizado correctamente.");
    }

    // AQUÍ SE HACE LA BAJA LÓGICA DE PRODUCTOS (DELETE /api/Products/{id}):
    // En lugar de eliminar la fila en MySQL, marca Discontinued = true para que
    // ya no se pueda vender en el POS pero siga apareciendo en las órdenes históricas.
    public async Task<ServiceResult<bool>> DescontinuarAsync(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null)
            return ServiceResult<bool>.Fail(404, "Producto no encontrado.");

        if (product.Discontinued == true)
            return ServiceResult<bool>.Ok(true, 200, "El producto ya se encontraba descontinuado.");

        product.Discontinued = true;
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, 200, "Producto descontinuado correctamente.");
    }

    // Obtiene la lista de categorías para los selectores del catálogo
    public async Task<ServiceResult<List<CategoryResponseDto>>> ObtenerCategoriasAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description
            })
            .ToListAsync();

        return ServiceResult<List<CategoryResponseDto>>.Ok(categories);
    }

    // Obtiene la lista de proveedores para el formulario de alta/edición de productos
    public async Task<ServiceResult<List<SupplierResponseDto>>> ObtenerProveedoresAsync()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .Select(s => new SupplierResponseDto
            {
                SupplierId = s.SupplierId,
                CompanyName = s.CompanyName
            })
            .ToListAsync();

        return ServiceResult<List<SupplierResponseDto>>.Ok(suppliers);
    }

    private async Task<string?> ValidateForeignKeysAsync(int? categoryId, int? supplierId)
    {
        if (categoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId.Value);
            if (!categoryExists)
                return "La categoría especificada no existe.";
        }

        if (supplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.SupplierId == supplierId.Value);
            if (!supplierExists)
                return "El proveedor especificado no existe.";
        }

        return null;
    }

    private static ProductResponseDto MapToDto(Product product) => new()
    {
        ProductId = product.ProductId,
        ProductName = product.ProductName,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.CompanyName,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.CategoryName,
        QuantityPerUnit = product.QuantityPerUnit,
        UnitPrice = product.UnitPrice ?? 0m,
        UnitsInStock = product.UnitsInStock ?? 0,
        Discontinued = product.Discontinued ?? false
    };
}
