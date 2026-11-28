// ============================================================================
// ARCHIVO: DTOs/Orders/CreateOrderRequestDto.cs
// PROPÓSITO: DTO de entrada para registrar una venta en POST /api/Orders.
//            POR QUÉ SOLO RECIBE CustomerId E Items:
//            - Si compra un Customer, incluso CustomerId se ignora y se toma del JWT.
//            - Si vende un Employee/Admin, envía el CustomerId seleccionado en pos.html.
//            - La dirección/destino de envío y los precios NO se piden aquí;
//              OrderService los toma directamente de las tablas 'customers' y 'products' en MySQL.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Orders;

public class CreateOrderRequestDto
{
    [StringLength(5, ErrorMessage = "El CustomerId no puede exceder los 5 caracteres.")]
    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "La venta debe incluir al menos un producto.")]
    [MinLength(1, ErrorMessage = "La venta debe incluir al menos un producto.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
