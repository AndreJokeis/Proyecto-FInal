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

    public async Task<ServiceResult<List<CustomerResponseDto>>> GetAllAsync()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.CompanyName)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ServiceResult<List<CustomerResponseDto>>.Ok(customers);
    }

    public async Task<ServiceResult<CustomerResponseDto>> GetByIdAsync(string customerId)
    {
        var id = customerId.Trim().ToUpperInvariant();
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<CustomerResponseDto>.Fail(404, "Cliente no encontrado.");

        return ServiceResult<CustomerResponseDto>.Ok(ToDto(customer));
    }

    public async Task<ServiceResult<CustomerResponseDto>> CreateAsync(CreateCustomerRequestDto request)
    {
        var id = request.CustomerId.Trim().ToUpperInvariant();

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

    public async Task<ServiceResult<CustomerResponseDto>> UpdateAsync(string customerId, UpdateCustomerRequestDto request)
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

    public async Task<ServiceResult<bool>> SoftDeleteAsync(string customerId)
    {
        var id = customerId.Trim().ToUpperInvariant();
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == id && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<bool>.Fail(404, "Cliente no encontrado.");

        // Eliminación lógica obligatoria: nunca usar Remove()
        customer.IsDeleted = true;
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, 200, "Cliente dado de baja correctamente.");
    }

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