using APIWeb.DTOs.Customers;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Employee")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _customerService.ObtenerTodosAsync();
        return Ok(result.Data);
    }

    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetById(string customerId)
    {
        var result = await _customerService.ObtenerPorIdAsync(customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequestDto request)
    {
        var result = await _customerService.CrearAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpPut("{customerId}")]
    public async Task<IActionResult> Update(string customerId, [FromBody] UpdateCustomerRequestDto request)
    {
        var result = await _customerService.EditarAsync(customerId, request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    [HttpDelete("{customerId}")]
    public async Task<IActionResult> Delete(string customerId)
    {
        var result = await _customerService.SoftDeleteAsync(customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(new { success = true, message = result.Message });
    }
}