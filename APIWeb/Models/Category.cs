// ============================================================================
// ARCHIVO: Models/Category.cs
// PROPÓSITO: Entidad de la tabla 'categories' de Northwind en MySQL.
//            Se utiliza para clasificar los productos y poblar los filtros/selectores
//            del catálogo mediante CategoryResponseDto (sin enviar la columna Picture).
// ============================================================================

namespace APIWeb.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public byte[]? Picture { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
