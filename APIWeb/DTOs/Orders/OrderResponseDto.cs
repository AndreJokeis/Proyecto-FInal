// ============================================================================
// ARCHIVO: DTOs/Orders/OrderResponseDto.cs
// PROPÓSITO: DTO de salida para el historial y detalle de órdenes (GET /api/Orders).
//            AQUÍ VIAJAN A ventas.js:
//            - Cliente: CustomerId y CompanyName (tomados de la tabla 'customers').
//            - Quién vendió: EmployeeId y EmployeeName (tomados de la tabla 'employees'
//              mediante JOIN/Include con Order.Employee).
//            - Destino: ShipAddress, ShipCity y ShipCountry (tomados de la orden o del
//              registro del cliente en la tabla 'customers').
//            - Totales y productos: Items y Total calculados en el servidor.
// ============================================================================

namespace APIWeb.DTOs.Orders;

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string? CustomerId { get; set; }
    public string? CompanyName { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipCountry { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}
