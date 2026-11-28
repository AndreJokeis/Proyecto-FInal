// ============================================================================
// ARCHIVO: Models/OrderDetail.cs
// PROPÓSITO: Entidad de la tabla 'order details' (detalle de productos por orden).
//            REGLA DE INTEGRIDAD DE PRECIOS:
//            El campo UnitPrice que se guarda aquí NUNCA se recibe desde el Frontend;
//            OrderService lo copia directamente de Product.UnitPrice en MySQL al
//            momento de confirmar la venta para evitar falsificación de precios.
// ============================================================================

namespace APIWeb.Models;

public partial class OrderDetail
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public float Discount { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
