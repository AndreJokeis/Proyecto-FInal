// ============================================================================
// ARCHIVO: Services/Interfaces/IEmployeeService.cs
// PROPÓSITO: Contrato del servicio para el alta exclusiva de empleados y
//            administradores en las tablas 'employees' y 'usuarios'.
// ============================================================================

using APIWeb.DTOs.Employees;

namespace APIWeb.Services.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<EmployeeResponseDto>> CrearEmpleadoAsync(CreateEmployeeRequestDto request);
}
