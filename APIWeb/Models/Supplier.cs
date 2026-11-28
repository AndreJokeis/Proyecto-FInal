// ============================================================================
// ARCHIVO: Models/Supplier.cs
// PROPÓSITO: Entidad de la tabla 'suppliers' (proveedores) de Northwind en MySQL.
//            Se expone hacia el formulario de productos únicamente con su ID y nombre
//            de empresa mediante SupplierResponseDto (mínima exposición de datos).
// ============================================================================

namespace APIWeb.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? ContactTitle { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    public string? HomePage { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
