using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Orders;

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del producto no es válido.")]
    public int ProductId { get; set; }

    [Range(1, short.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Quantity { get; set; }
}
