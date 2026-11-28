// ============================================================================
// ARCHIVO: Services/OrderService.cs
// PROPÓSITO: Servicio central de Ventas (Punto de Venta e Historial de Órdenes).
//            AQUÍ SE RESUELVEN LAS REGLAS MÁS IMPORTANTES DE LA REVISIÓN:
//            1. ¿DE DÓNDE SALE QUIÉN VENDIÓ (Empleado)?
//               - Al crear la venta (CrearOrdenAsync), si el usuario autenticado es
//                 Employee o Admin, se toma su 'EmployeeId' desde el Claim del JWT
//                 y se consulta la tabla 'employees' en MySQL.
//               - Al consultar órdenes (ObtenerOrdenAsync / ObtenerPorIdAsync), se hace
//                 .Include(o => o.Employee) para unir la tabla 'orders' con 'employees'
//                 y armar EmployeeName = FirstName + " " + LastName.
//            2. ¿DE DÓNDE SALE EL DESTINO?
//               - Se toma directamente de la base de datos (tabla 'customers':
//                 Address, City, Region, PostalCode, Country), sin pedirlo en campos
//                 de texto en el POS.
//               - Si una orden antigua tenía ShipCity/ShipCountry en NULL pero al cliente
//                 se le actualizó su dirección en la tabla 'customers', MapToDto toma
//                 automáticamente la dirección/ciudad/país actual del cliente en la BD.
//            3. ¿CÓMO SE EVITA FALSIFICAR PRECIOS, COMPRAR DESCONTINUADOS O SIN STOCK?
//               - Se prohíben productos repetidos en la misma orden.
//               - Se verifica en MySQL que product.Discontinued != true.
//               - Se verifica que product.UnitsInStock >= item.Quantity y se descuenta.
//               - El precio unitario (unitPrice) se toma EXCLUSIVAMENTE de
//                 product.UnitPrice en MySQL (Server-Side Truth).
// ============================================================================

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

    // ------------------------------------------------------------------------
    // HISTORIAL DE VENTAS / MIS PEDIDOS (GET /api/Orders)
    // PROCESO:
    // - Hace JOIN (.Include) con Customer (cliente), Employee (quién vendió)
    //   y OrderDetails -> Product (productos vendidos).
    // - Si el rol en el JWT es "Customer", filtra la consulta por su CustomerId
    //   para que un cliente JAMÁS pueda ver pedidos de otros clientes.
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<List<OrderResponseDto>>> ObtenerOrdenAsync(string role, string? customerId)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        // Filtro de seguridad por rol: el cliente solo ve sus propias órdenes
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

        var result = orders.Select(MapToDto).ToList();

        return ServiceResult<List<OrderResponseDto>>.Ok(result);
    }

    // ------------------------------------------------------------------------
    // DETALLE DE UNA ORDEN POR ID (GET /api/Orders/{orderId})
    // PROCESO:
    // - Busca la orden incluyendo Customer, Employee y OrderDetails -> Product.
    // - Si el usuario es "Customer" y la orden pertenece a otro CustomerId,
    //   la API bloquea el acceso devolviendo HTTP 403 Forbidden.
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<OrderResponseDto>> ObtenerPorIdAsync(int orderId, string role, string? customerId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
            return ServiceResult<OrderResponseDto>.Fail(404, "Orden no encontrada.");

        // Protección contra acceso horizontal (IDOR): un cliente no puede abrir órdenes ajenas
        if (role == "Customer" && order.CustomerId != customerId)
            return ServiceResult<OrderResponseDto>.Fail(403, "No tienes permiso para consultar esta orden.");

        return ServiceResult<OrderResponseDto>.Ok(MapToDto(order));
    }

    // ------------------------------------------------------------------------
    // REGISTRO TRANSACCIONAL DE VENTA EN PUNTO DE VENTA (POST /api/Orders)
    // DE DÓNDE VIENE LA PETICIÓN: De pos.js al presionar "Confirmar venta / compra".
    // PROCESO PASO A PASO:
    // 1. Valida que la lista de productos no esté vacía ni tenga IDs repetidos.
    // 2. Determina el cliente:
    //    - Si quien compra es "Customer", ignora cualquier CustomerId del body y usa el del JWT.
    //    - Si quien vende es "Employee" o "Admin", usa el CustomerId seleccionado en pos.html.
    // 3. Verifica en la tabla 'customers' que el cliente exista y no tenga baja lógica (!c.IsDeleted).
    // 4. Determina quién vendió (seller): si es Employee/Admin y su JWT tiene EmployeeId,
    //    obtiene el registro del empleado desde la tabla 'employees'.
    // 5. Abre una transacción en MySQL:
    //    - Verifica que cada producto exista, NO esté descontinuado y tenga stock suficiente.
    //    - Toma UnitPrice desde la tabla 'products' en MySQL y resta UnitsInStock.
    //    - Copia los datos de destino (ShipName, ShipAddress, ShipCity, ShipRegion,
    //      ShipPostalCode, ShipCountry) directamente desde el cliente en la BD.
    //    - Inserta en 'orders' y 'order details' y confirma la transacción (Commit).
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<OrderResponseDto>> CrearOrdenAsync(
        CreateOrderRequestDto request,
        string role,
        string? jwtCustomerId,
        int? jwtEmployeeId)
    {
        if (request.Items is null || request.Items.Count == 0)
            return ServiceResult<OrderResponseDto>.Fail(400, "La venta debe incluir al menos un producto.");

        // REGLA DE NEGOCIO (Evitar duplicados): no se permite enviar dos renglones con el mismo ProductId
        var hasDuplicates = request.Items
            .GroupBy(i => i.ProductId)
            .Any(g => g.Count() > 1);

        if (hasDuplicates)
            return ServiceResult<OrderResponseDto>.Fail(400, "No se permiten productos duplicados en la misma orden.");

        // Resolución segura del CustomerId según el rol del JWT
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

        // Consulta el cliente en la BD verificando que no haya sido dado de baja lógica
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == resolvedCustomerId && !c.IsDeleted);

        if (customer is null)
            return ServiceResult<OrderResponseDto>.Fail(404, "El cliente especificado no existe o fue dado de baja.");

        // AQUÍ SE OBTIENE QUIÉN VENDIÓ DESDE LA TABLA 'employees':
        // Si la venta la registra un empleado o administrador, buscamos su registro por jwtEmployeeId
        Employee? seller = null;
        if ((role == "Admin" || role == "Employee") && jwtEmployeeId.HasValue)
        {
            seller = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == jwtEmployeeId.Value);
        }

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

                // REGLA DE NEGOCIO: Bloquear compra de productos descontinuados
                if (product.Discontinued == true)
                    return ServiceResult<OrderResponseDto>.Fail(
                        400,
                        $"No es posible completar la venta: el producto '{product.ProductName}' está descontinuado.");

                // REGLA DE NEGOCIO: Validar existencias suficientes antes de vender
                var currentStock = product.UnitsInStock ?? 0;
                if (currentStock < item.Quantity)
                    return ServiceResult<OrderResponseDto>.Fail(
                        400,
                        $"Stock insuficiente para '{product.ProductName}'. Disponible: {currentStock}, solicitado: {item.Quantity}.");

                // REGLA DE NEGOCIO (Server-Side Truth / Evitar falsificación de precios):
                // El precio unitario se lee directamente del producto en MySQL y se descuenta el stock.
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

            // AQUÍ SE TOMA EL DESTINO DIRECTAMENTE DE LA BASE DE DATOS (tabla 'customers'):
            // ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode y ShipCountry
            // se copian del registro del cliente almacenado en MySQL.
            var now = DateTime.UtcNow;
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                EmployeeId = seller?.EmployeeId,
                OrderDate = now,
                ShipName = customer.CompanyName,
                ShipAddress = customer.Address,
                ShipCity = customer.City,
                ShipRegion = customer.Region,
                ShipPostalCode = customer.PostalCode,
                ShipCountry = customer.Country,
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
                EmployeeName = FormatEmployeeName(seller),
                OrderDate = order.OrderDate,
                ShipAddress = order.ShipAddress,
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

    // Une FirstName y LastName de la entidad Employee (tabla 'employees')
    private static string? FormatEmployeeName(Employee? employee)
    {
        if (employee is null)
            return null;

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    // Convierte la entidad Order de EF Core a OrderResponseDto.
    // NOTA SOBRE EL DESTINO: Si la orden se creó cuando el cliente aún no tenía
    // dirección en 'orders', toma como respaldo Address, City y Country actuales de order.Customer.
    private static OrderResponseDto MapToDto(Order order) => new()
    {
        OrderId = order.OrderId,
        CustomerId = order.CustomerId,
        CompanyName = order.Customer?.CompanyName,
        EmployeeId = order.EmployeeId,
        EmployeeName = FormatEmployeeName(order.Employee),
        OrderDate = order.OrderDate,
        ShipAddress = string.IsNullOrWhiteSpace(order.ShipAddress) ? order.Customer?.Address : order.ShipAddress,
        ShipCity = string.IsNullOrWhiteSpace(order.ShipCity) ? order.Customer?.City : order.ShipCity,
        ShipCountry = string.IsNullOrWhiteSpace(order.ShipCountry) ? order.Customer?.Country : order.ShipCountry,
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
}
