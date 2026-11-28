// ============================================================================
// ARCHIVO: Services/CustomerService.cs
// PROPÓSITO: Lógica de negocio para el CRUD de Clientes y su Eliminación Lógica.
//            AQUÍ SE HACE:
//            1. ObtenerTodosAsync / ObtenerPorIdAsync: Consulta clientes activos
//               filtrando siempre por !c.IsDeleted.
//            2. CrearAsync: Valida que el CustomerId no esté duplicado (409 Conflict)
//               y sanitiza las cadenas con .Trim().
//            3. EditarAsync: Actualiza datos de contacto y dirección/ciudad/país
//               (que también sirven como destino de envío en las órdenes del cliente).
//            4. SoftDeleteAsync: Realiza la BAJA LÓGICA (customer.IsDeleted = true)
//               y desactiva su usuario (linkedUser.IsActive = false) sin borrar el
//               registro físico de MySQL para no destruir su historial de ventas.
// ============================================================================

using APIWeb.Data;
using APIWeb.DTOs.Customers;
using APIWeb.Models;
using APIWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Services;

public class CustomerService : ICustomerService
{
    private readonly ContextoBD _context;

    public CustomerService(ContextoBD context)
    {
        _context = context;
    }

    // Lista únicamente los clientes activos (IsDeleted == false) ordenados por nombre de empresa
    public async Task<ServiceResult<List<CustomerResponseDto>>> ObtenerTodosAsync()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.CompanyName)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ServiceResult<List<CustomerResponseDto>>.Ok(customers);
    }

    // Busca un cliente activo por su CustomerId
    public async Task<ServiceResult<CustomerResponseDto>> ObtenerPorIdAsync(string customerId)
    {
        var id = customerId.Trim().ToUpperInvariant();
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<CustomerResponseDto>.Fail(404, "Cliente no encontrado.");

        return ServiceResult<CustomerResponseDto>.Ok(ToDto(customer));
    }

    // Alta de cliente con validación anti-duplicados (HTTP 409 Conflict)
    public async Task<ServiceResult<CustomerResponseDto>> CrearAsync(CreateCustomerRequestDto request)
    {
        var id = request.CustomerId.Trim().ToUpperInvariant();

        // REGLA DE NEGOCIO (Evitar duplicados): verifica si el ID ya existe en 'customers'
        var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
        if (exists)
            return ServiceResult<CustomerResponseDto>.Fail(409, "Ya existe un cliente registrado con ese ID.");

        var customer = new Customer
        {
            CustomerId = id,
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName?.Trim(),
            ContactTitle = request.ContactTitle?.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            Region = request.Region?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim(),
            Phone = request.Phone?.Trim(),
            IsDeleted = false
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return ServiceResult<CustomerResponseDto>.Ok(ToDto(customer), 201, "Cliente creado correctamente.");
    }

    // Actualiza los datos de un cliente activo (incluyendo Ciudad, País y Dirección que usa el POS como destino)
    public async Task<ServiceResult<CustomerResponseDto>> EditarAsync(string customerId, UpdateCustomerRequestDto request)
    {
        var id = customerId.Trim().ToUpperInvariant();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<CustomerResponseDto>.Fail(404, "Cliente no encontrado.");

        customer.CompanyName = request.CompanyName.Trim();
        customer.ContactName = request.ContactName?.Trim();
        customer.ContactTitle = request.ContactTitle?.Trim();
        customer.Address = request.Address?.Trim();
        customer.City = request.City?.Trim();
        customer.Region = request.Region?.Trim();
        customer.PostalCode = request.PostalCode?.Trim();
        customer.Country = request.Country?.Trim();
        customer.Phone = request.Phone?.Trim();

        await _context.SaveChangesAsync();

        return ServiceResult<CustomerResponseDto>.Ok(ToDto(customer), 200, "Cliente actualizado correctamente.");
    }

    // ------------------------------------------------------------------------
    // AQUÍ SE HACE LA ELIMINACIÓN LÓGICA DE CLIENTES (DELETE /api/customers/{id})
    // POR QUÉ NO SE USA _context.Customers.Remove(customer):
    // Porque en la tabla 'orders' existen ventas históricas asociadas a este CustomerId.
    // Si se borrara físicamente, se perdería el historial o fallaría la llave foránea.
    // PROCESO:
    // 1. Marca customer.IsDeleted = true (deja de aparecer en el catálogo y en el POS).
    // 2. Si el cliente tenía cuenta en 'usuarios', marca linkedUser.IsActive = false
    //    para impedir que vuelva a iniciar sesión.
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<bool>> SoftDeleteAsync(string customerId)
    {
        var id = customerId.Trim().ToUpperInvariant();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<bool>.Fail(404, "Cliente no encontrado.");

        customer.IsDeleted = true;

        var linkedUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.CustomerId == id);
        if (linkedUser is not null)
            linkedUser.IsActive = false;

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, 200, "Cliente dado de baja correctamente.");
    }

    // Convierte la entidad Customer a CustomerResponseDto para exponer solo los campos necesarios
    private static CustomerResponseDto ToDto(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        CompanyName = c.CompanyName,
        ContactName = c.ContactName,
        ContactTitle = c.ContactTitle,
        Address = c.Address,
        City = c.City,
        Region = c.Region,
        PostalCode = c.PostalCode,
        Country = c.Country,
        Phone = c.Phone
    };
}
