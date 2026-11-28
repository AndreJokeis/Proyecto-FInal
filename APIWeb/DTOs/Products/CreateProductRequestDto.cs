// ============================================================================
// ARCHIVO: DTOs/Products/CreateProductRequestDto.cs
// PROPÓSITO: DTO de entrada para crear un producto (POST /api/Products).
//            Solo incluye los campos necesarios del negocio y valida que
//            UnitPrice y UnitsInStock no sean negativos.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Products;

public class CreateProductRequestDto
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(40, ErrorMessage = "El nombre del producto no puede exceder los 40 caracteres.")]
    public string ProductName { get; set; } = string.Empty;

    public int? SupplierId { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(20, ErrorMessage = "La cantidad por unidad no puede exceder los 20 caracteres.")]
    public string? QuantityPerUnit { get; set; }

    [Range(0, 999999.99, ErrorMessage = "El precio unitario debe ser mayor o igual a 0.")]
    public decimal UnitPrice { get; set; }

    [Range(0, short.MaxValue, ErrorMessage = "Las unidades en stock deben ser mayores o iguales a 0.")]
    public int UnitsInStock { get; set; }

    public bool Discontinued { get; set; }
}
