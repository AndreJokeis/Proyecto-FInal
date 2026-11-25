using System.Security.Claims;
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
}