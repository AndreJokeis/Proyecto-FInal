// ============================================================================
// ARCHIVO: Services/Interfaces/IProductService.cs
// PROPÓSITO: Contrato del servicio de catálogo de productos, categorías,
//            proveedores y descontinuación lógica.
// ============================================================================

using APIWeb.DTOs.Products;

namespace APIWeb.Services.Interfaces;

public interface IProductService
{
    Task<ServiceResult<List<ProductResponseDto>>> ObtenerTodosAsync(bool includeDiscontinued = false);
    Task<ServiceResult<ProductResponseDto>> ObtenerPorIdAsync(int id);
    Task<ServiceResult<ProductResponseDto>> CrearAsync(CreateProductRequestDto request);
    Task<ServiceResult<ProductResponseDto>> EditarAsync(int id, UpdateProductRequestDto request);
    Task<ServiceResult<bool>> DescontinuarAsync(int id);
    Task<ServiceResult<List<CategoryResponseDto>>> ObtenerCategoriasAsync();
    Task<ServiceResult<List<SupplierResponseDto>>> ObtenerProveedoresAsync();
}
