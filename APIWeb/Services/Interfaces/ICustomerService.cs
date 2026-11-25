using APIWeb.DTOs.Customers;

namespace APIWeb.Services.Interfaces;

public interface ICustomerService
{
    Task<ServiceResult<List<CustomerResponseDto>>> GetAllAsync();
    Task<ServiceResult<CustomerResponseDto>> GetByIdAsync(string customerId);
    Task<ServiceResult<CustomerResponseDto>> CreateAsync(CreateCustomerRequestDto request);
    Task<ServiceResult<CustomerResponseDto>> UpdateAsync(string customerId, UpdateCustomerRequestDto request);
    Task<ServiceResult<bool>> SoftDeleteAsync(string customerId);
}