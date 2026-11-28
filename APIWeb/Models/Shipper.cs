// ============================================================================
// ARCHIVO: Models/Shipper.cs
// PROPÓSITO: Entidad de la tabla 'shippers' (paqueterías) de Northwind en MySQL.
//            Mantiene la relación de llave foránea ShipVia de la tabla 'orders'.
// ============================================================================

namespace APIWeb.Models;

public partial class Shipper
{
    public int ShipperId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? Phone { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
