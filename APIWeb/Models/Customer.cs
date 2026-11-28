// ============================================================================
// ARCHIVO: Models/Customer.cs
// PROPÓSITO: Entidad de la tabla 'customers' de Northwind en MySQL.
//            DE DÓNDE SE TOMA EL DESTINO DE LAS ÓRDENES:
//            De los campos Address, City, Region, PostalCode y Country de esta tabla.
//            REGLA DE BAJA LÓGICA (IsDeleted):
//            Se agregó la columna IsDeleted para que, al eliminar un cliente en el
//            sistema, solo se marque IsDeleted = true sin borrar la fila física en
//            MySQL, preservando así todas sus órdenes históricas en la tabla 'orders'.
// ============================================================================

namespace APIWeb.Models;

public partial class Customer
{
    // ID alfanumérico de 3 a 5 caracteres (ej. "ALFKI" o generado automáticamente al registrarse)
    public string CustomerId { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? ContactTitle { get; set; }

    // Dirección, ciudad, región, código postal y país del cliente (usados como destino en sus compras)
    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? Phone { get; set; }

    public string? Fax { get; set; }

    public byte[]? Image { get; set; }

    public byte[]? ImageThumbnail { get; set; }

    // Bandera de eliminación lógica: false = cliente activo, true = dado de baja
    public bool IsDeleted { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
