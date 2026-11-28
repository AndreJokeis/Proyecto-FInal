// ============================================================================
// ARCHIVO: Models/Product.cs
// PROPÓSITO: Entidad de la tabla 'products' de Northwind en MySQL.
//            REGLAS DE NEGOCIO ASOCIADAS:
//            - UnitPrice: Precio oficial del producto en la BD (fuente única de verdad).
//            - UnitsInStock: Existencias actuales; se valida y descuenta al vender.
//            - Discontinued: Baja lógica de productos. Si es true, no se elimina de la BD
//              (para no romper 'order details' históricos), pero se bloquea su venta.
// ============================================================================

namespace APIWeb.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int? SupplierId { get; set; }

    public int? CategoryId { get; set; }

    public string? QuantityPerUnit { get; set; }

    public decimal? UnitPrice { get; set; }

    public int? UnitsInStock { get; set; }

    public int? UnitsOnOrder { get; set; }

    public int? ReorderLevel { get; set; }

    public bool? Discontinued { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Supplier? Supplier { get; set; }
}
