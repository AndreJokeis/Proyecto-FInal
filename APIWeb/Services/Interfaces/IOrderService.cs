// ============================================================================
// ARCHIVO: Services/Interfaces/IOrderService.cs
// PROPÓSITO: Contrato del servicio de órdenes (consulta de historial filtrado
//            por rol, detalle de orden y registro transaccional de ventas POS).
// ============================================================================

using APIWeb.DTOs.Orders;

namespace APIWeb.Services.Interfaces;

public interface IOrderService
{
    Task<ServiceResult<List<OrderResponseDto>>> ObtenerOrdenAsync(string role, string? customerId);
    Task<ServiceResult<OrderResponseDto>> ObtenerPorIdAsync(int orderId, string role, string? customerId);
    Task<ServiceResult<OrderResponseDto>> CrearOrdenAsync(CreateOrderRequestDto request, string role, string? jwtCustomerId, int? jwtEmployeeId);
}
