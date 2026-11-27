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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto request)
    {
        var result = await _employeeService.CrearEmpleadoAsync(request);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message, errors = Array.Empty<string>() });

        return StatusCode(result.StatusCode, result.Data);
    }
}
