using APIWeb.DTOs.Orders;

namespace APIWeb.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<List<OrderResponseDto>>> ObtenerOrdenAsync(string role, string? customerId);
    Task<ServiceResult<OrderResponseDto>> ObtenerPorIdAsync(int orderId, string role, string? customerId);
}