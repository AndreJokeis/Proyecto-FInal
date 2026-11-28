// ============================================================================
// ARCHIVO: Models/Order.cs
// PROPÓSITO: Entidad de la tabla 'orders' (encabezado de cada venta/pedido) en MySQL.
//            RELACIONES CLAVE:
//            - CustomerId -> Customer (cliente al que pertenece la compra).
//            - EmployeeId -> Employee (empleado/admin que registró la venta en POS,
//                            o null si fue una compra directa del cliente en línea).
//            - ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry ->
//              datos de destino tomados directamente del registro del cliente en la BD.
//            - OrderDetails -> renglones de productos vendidos en esta orden.
// ============================================================================

namespace APIWeb.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string? CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? RequiredDate { get; set; }

    public DateTime? ShippedDate { get; set; }

    public int? ShipVia { get; set; }

    public decimal? Freight { get; set; }

    public string? ShipName { get; set; }

    public string? ShipAddress { get; set; }

    public string? ShipCity { get; set; }

    public string? ShipRegion { get; set; }

    public string? ShipPostalCode { get; set; }

    public string? ShipCountry { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Shipper? ShipViaNavigation { get; set; }
}
