// ============================================================================
// ARCHIVO: Controllers/OrderController.cs (OrdersController)
// PROPÓSITO: Endpoints de Ventas y Pedidos (/api/Orders).
//            CÓMO TOMA LOS DATOS DEL USUARIO AUTENTICADO:
//            Lee directamente los Claims firmados dentro del Token JWT:
//            - ClaimTypes.Role: Rol del usuario ("Admin", "Employee" o "Customer").
//            - "CustomerId": ID del cliente (si el usuario es Customer).
//            - "EmployeeId": ID del empleado en la tabla 'employees' (si es Employee o Admin)
//              para registrar automáticamente quién realizó la venta.
// ============================================================================

using System.Security.Claims;
using APIWeb.DTOs.Orders;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Employee,Customer")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET /api/Orders -> Historial de ventas (todas para Admin/Employee; solo las propias para Customer)
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var customerId = User.FindFirstValue("CustomerId");

        var result = await _orderService.ObtenerOrdenAsync(role, customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // GET /api/Orders/{orderId} -> Detalle de una orden específica (usado por el modal de ventas.js)
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(int orderId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var customerId = User.FindFirstValue("CustomerId");

        var result = await _orderService.ObtenerPorIdAsync(orderId, role, customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // POST /api/Orders -> Registra una nueva venta desde el Punto de Venta (pos.js)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var customerId = User.FindFirstValue("CustomerId");
        var employeeIdClaim = User.FindFirstValue("EmployeeId");
        int? employeeId = int.TryParse(employeeIdClaim, out var parsedEmployeeId) ? parsedEmployeeId : null;

        var result = await _orderService.CrearOrdenAsync(request, role, customerId, employeeId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }
}
