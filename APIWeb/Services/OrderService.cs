using APIWeb.Data;
using APIWeb.DTOs.Orders;
using APIWeb.Models;
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

    public async Task<ServiceResult<OrderResponseDto>> CrearOrdenAsync(
        CreateOrderRequestDto request,
        string role,
        string? jwtCustomerId,
        int? jwtEmployeeId)
    {
        if (request.Items is null || request.Items.Count == 0)
            return ServiceResult<OrderResponseDto>.Fail(400, "La venta debe incluir al menos un producto.");

        var hasDuplicates = request.Items
            .GroupBy(i => i.ProductId)
            .Any(g => g.Count() > 1);

        if (hasDuplicates)
            return ServiceResult<OrderResponseDto>.Fail(400, "No se permiten productos duplicados en la misma orden.");

        string? resolvedCustomerId;
        if (role == "Customer")
        {
            if (string.IsNullOrWhiteSpace(jwtCustomerId))
                return ServiceResult<OrderResponseDto>.Fail(403, "Tu cuenta no tiene un perfil de cliente asociado.");

            resolvedCustomerId = jwtCustomerId.Trim();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                return ServiceResult<OrderResponseDto>.Fail(400, "Debe especificar el CustomerId del cliente para registrar la venta.");

            resolvedCustomerId = request.CustomerId.Trim().ToUpperInvariant();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == resolvedCustomerId && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<OrderResponseDto>.Fail(404, "El cliente especificado no existe o fue dado de baja.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var productsById = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            var orderDetails = new List<OrderDetail>();

            foreach (var item in request.Items)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                    return ServiceResult<OrderResponseDto>.Fail(404, $"El producto con ID {item.ProductId} no existe.");

                if (product.Discontinued == true)
                    return ServiceResult<OrderResponseDto>.Fail(
                        400,
                        $"No es posible completar la venta: el producto '{product.ProductName}' está descontinuado.");

                var currentStock = product.UnitsInStock ?? 0;
                if (currentStock < item.Quantity)
                    return ServiceResult<OrderResponseDto>.Fail(
                        400,
                        $"Stock insuficiente para '{product.ProductName}'. Disponible: {currentStock}, solicitado: {item.Quantity}.");

                // Server-Side Truth: el precio unitario se toma exclusivamente de la base de datos.
                var unitPrice = product.UnitPrice ?? 0m;
                product.UnitsInStock = currentStock - item.Quantity;

                orderDetails.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    Discount = 0f
                });
            }

            var now = DateTime.UtcNow;
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                EmployeeId = (role == "Admin" || role == "Employee") ? jwtEmployeeId : null,
                OrderDate = now,
                ShipName = string.IsNullOrWhiteSpace(request.ShipName) ? customer.CompanyName : request.ShipName.Trim(),
                ShipAddress = string.IsNullOrWhiteSpace(request.ShipAddress) ? customer.Address : request.ShipAddress.Trim(),
                ShipCity = string.IsNullOrWhiteSpace(request.ShipCity) ? customer.City : request.ShipCity.Trim(),
                ShipRegion = string.IsNullOrWhiteSpace(request.ShipRegion) ? customer.Region : request.ShipRegion.Trim(),
                ShipPostalCode = string.IsNullOrWhiteSpace(request.ShipPostalCode) ? customer.PostalCode : request.ShipPostalCode.Trim(),
                ShipCountry = string.IsNullOrWhiteSpace(request.ShipCountry) ? customer.Country : request.ShipCountry.Trim(),
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var responseDto = new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CompanyName = customer.CompanyName,
                EmployeeId = order.EmployeeId,
                OrderDate = order.OrderDate,
                ShipCity = order.ShipCity,
                ShipCountry = order.ShipCountry,
                Items = orderDetails.Select(od => new OrderItemResponseDto
                {
                    ProductId = od.ProductId,
                    ProductName = productsById[od.ProductId].ProductName,
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Subtotal = od.UnitPrice * od.Quantity
                }).ToList(),
                Total = orderDetails.Sum(od => od.UnitPrice * od.Quantity)
            };

            return ServiceResult<OrderResponseDto>.Ok(responseDto, 201, "Venta registrada correctamente.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}