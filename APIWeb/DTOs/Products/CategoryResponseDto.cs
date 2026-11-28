// ============================================================================
// ARCHIVO: DTOs/Products/CategoryResponseDto.cs
// PROPÓSITO: DTO ligero de salida para GET /api/Categories.
//            Envía únicamente CategoryId, CategoryName y Description (sin el
//            arreglo de bytes Picture de la BD) para llenar el filtro de categorías.
// ============================================================================

namespace APIWeb.DTOs.Products;

public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
