// ============================================================================
// ARCHIVO: DTOs/Orders/OrderItemResponseDto.cs
// PROPÓSITO: DTO de salida con el detalle de cada producto dentro de una orden
//            (nombre, precio oficial aplicado, cantidad y subtotal calculado).
// ============================================================================

namespace APIWeb.DTOs.Orders;

public class OrderItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}
