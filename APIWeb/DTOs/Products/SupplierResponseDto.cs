// ============================================================================
// ARCHIVO: DTOs/Products/SupplierResponseDto.cs
// PROPÓSITO: DTO ligero de salida para GET /api/Suppliers.
//            Expone únicamente SupplierId y CompanyName para poblar el selector
//            de proveedores en el modal de productos.
// ============================================================================

namespace APIWeb.DTOs.Products;

public class SupplierResponseDto
{
    public int SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
