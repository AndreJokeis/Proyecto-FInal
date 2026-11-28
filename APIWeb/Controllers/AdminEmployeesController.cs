// ============================================================================
// ARCHIVO: Controllers/AdminEmployeesController.cs
// PROPÓSITO: Endpoint exclusivo para que un Administrador registre empleados o
//            administradores (POST /api/admin/employees).
//            AUTORIZACIÓN EN LA API:
//            [Authorize(Roles = "Admin")] exige un token JWT válido cuyo claim
//            de rol sea exclusivamente "Admin". Si un Employee o Customer intenta
//            llamar a este endpoint, ASP.NET Core devuelve HTTP 403 Forbidden.
// ============================================================================

using APIWeb.DTOs.Employees;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/admin/employees")]
[Authorize(Roles = "Admin")]
public class AdminEmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public AdminEmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // POST /api/admin/employees -> Crea registro en 'employees' y usuario en 'usuarios' (llamado desde empleados.js)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto request)
    {
        var result = await _employeeService.CrearEmpleadoAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }
}
