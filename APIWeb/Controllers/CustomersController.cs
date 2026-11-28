// ============================================================================
// ARCHIVO: Controllers/CustomersController.cs
// PROPÓSITO: Endpoints CRUD de Clientes con Baja Lógica (/api/customers).
//            AUTORIZACIÓN EN LA API:
//            [Authorize(Roles = "Admin,Employee")] permite el acceso únicamente
//            al personal (Admin y Employee); bloquea a usuarios con rol Customer (403).
// ============================================================================

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

    // GET /api/customers -> Lista todos los clientes activos (usado en clientes.js y en el selector de pos.js)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _customerService.ObtenerTodosAsync();
        return Ok(result.Data);
    }

    // GET /api/customers/{customerId} -> Consulta un cliente específico por su ID
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetById(string customerId)
    {
        var result = await _customerService.ObtenerPorIdAsync(customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // POST /api/customers -> Registra un nuevo cliente en la tabla 'customers'
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequestDto request)
    {
        var result = await _customerService.CrearAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }

    // PUT /api/customers/{customerId} -> Actualiza los datos y dirección de un cliente existente
    [HttpPut("{customerId}")]
    public async Task<IActionResult> Update(string customerId, [FromBody] UpdateCustomerRequestDto request)
    {
        var result = await _customerService.EditarAsync(customerId, request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(result.Data);
    }

    // DELETE /api/customers/{customerId} -> Ejecuta la BAJA LÓGICA (IsDeleted = true) sin borrar de MySQL
    [HttpDelete("{customerId}")]
    public async Task<IActionResult> Delete(string customerId)
    {
        var result = await _customerService.SoftDeleteAsync(customerId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return Ok(new { success = true, message = result.Message });
    }
}
