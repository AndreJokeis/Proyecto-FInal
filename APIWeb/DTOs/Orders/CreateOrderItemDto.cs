// ============================================================================
// ARCHIVO: DTOs/Orders/CreateOrderItemDto.cs
// PROPÓSITO: Representa cada renglón del carrito que envía el Frontend al confirmar
//            una venta (POST /api/Orders).
//            REGLA DE SEGURIDAD CRÍTICA (Cero falsificación de precios):
//            Observa que este DTO SOLO contiene ProductId y Quantity.
//            NO existe propiedad UnitPrice ni Subtotal aquí, por lo que es imposible
//            que un usuario malicioso altere el precio desde el navegador o Postman.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Orders;

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del producto no es válido.")]
    public int ProductId { get; set; }

    [Range(1, short.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Quantity { get; set; }
}
