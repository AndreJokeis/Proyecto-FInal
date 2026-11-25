using APIWeb.Data;
using APIWeb.DTOs.Orders;
using APIWeb.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Services;

public class OrderService : IOrderService
{
    private readonly ContextoBD _context;

    public OrderService(ContextoBD context)
    {
        _context = context;
    }

    public async Task<ServiceResult<List<OrderResponseDto>>> ObtenerOrdenAsync(string role, string? customerId)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (role == "Customer")
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return ServiceResult<List<OrderResponseDto>>.Fail(403, "Tu cuenta no tiene un perfil de cliente asociado.");

            query = query.Where(o => o.CustomerId == customerId);
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderId)
            .Take(100)
            .ToListAsync();

        var result = orders.Select(o => new OrderResponseDto
        {
            OrderId = o.OrderId,
            CustomerId = o.CustomerId,
            CompanyName = o.Customer?.CompanyName,
            EmployeeId = o.EmployeeId,
            OrderDate = o.OrderDate,
            ShipCity = o.ShipCity,
            ShipCountry = o.ShipCountry,
            Items = o.OrderDetails.Select(od => new OrderItemResponseDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product?.ProductName ?? "Producto",
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Subtotal = od.UnitPrice * od.Quantity
            }).ToList(),
            Total = o.OrderDetails.Sum(od => od.UnitPrice * od.Quantity)
        }).ToList();

        return ServiceResult<List<OrderResponseDto>>.Ok(result);
    }

    public async Task<ServiceResult<OrderResponseDto>> ObtenerPorIdAsync(int orderId, string role, string? customerId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
            return ServiceResult<OrderResponseDto>.Fail(404, "Orden no encontrada.");

        if (role == "Customer" && order.CustomerId != customerId)
            return ServiceResult<OrderResponseDto>.Fail(403, "No tienes permiso para consultar esta orden.");

        var dto = new OrderResponseDto
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            CompanyName = order.Customer?.CompanyName,
            EmployeeId = order.EmployeeId,
            OrderDate = order.OrderDate,
            ShipCity = order.ShipCity,
            ShipCountry = order.ShipCountry,
            Items = order.OrderDetails.Select(od => new OrderItemResponseDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product?.ProductName ?? "Producto",
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                Subtotal = od.UnitPrice * od.Quantity
            }).ToList(),
            Total = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity)
        };

        return ServiceResult<OrderResponseDto>.Ok(dto);
    }
}