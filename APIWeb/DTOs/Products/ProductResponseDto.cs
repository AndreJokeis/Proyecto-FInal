namespace APIWeb.DTOs.Products;

public class ProductResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? QuantityPerUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitsInStock { get; set; }
    public bool Discontinued { get; set; }
}
