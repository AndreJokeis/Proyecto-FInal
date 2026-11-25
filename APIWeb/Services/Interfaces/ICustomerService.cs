using APIWeb.DTOs.Customers;

namespace APIWeb.Services.Interfaces;

public interface ICustomerService
{
    Task<ServiceResult<List<CustomerResponseDto>>> ObtenerTodosAsync();
    Task<ServiceResult<CustomerResponseDto>> ObtenerPorIdAsync(string customerId);
    Task<ServiceResult<CustomerResponseDto>> CrearAsync(CreateCustomerRequestDto request);
    Task<ServiceResult<CustomerResponseDto>> EditarAsync(string customerId, UpdateCustomerRequestDto request);
    Task<ServiceResult<bool>> SoftDeleteAsync(string customerId);
}