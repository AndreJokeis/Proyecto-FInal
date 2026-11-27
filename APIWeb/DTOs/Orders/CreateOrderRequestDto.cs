using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Orders;

public class CreateOrderRequestDto
{
    [StringLength(5, ErrorMessage = "El CustomerId no puede exceder los 5 caracteres.")]
    public string? CustomerId { get; set; }

    [StringLength(40)]
    public string? ShipName { get; set; }

    [StringLength(60)]
    public string? ShipAddress { get; set; }

    [StringLength(15)]
    public string? ShipCity { get; set; }

    [StringLength(15)]
    public string? ShipRegion { get; set; }

    [StringLength(10)]
    public string? ShipPostalCode { get; set; }

    [StringLength(15)]
    public string? ShipCountry { get; set; }

    [Required(ErrorMessage = "La venta debe incluir al menos un producto.")]
    [MinLength(1, ErrorMessage = "La venta debe incluir al menos un producto.")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
