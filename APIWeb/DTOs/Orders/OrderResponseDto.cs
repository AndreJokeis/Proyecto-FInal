namespace APIWeb.DTOs.Orders;

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string? CustomerId { get; set; }
    public string? CompanyName { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipCountry { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}