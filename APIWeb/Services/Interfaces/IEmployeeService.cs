using APIWeb.DTOs.Employees;

namespace APIWeb.Services.Interfaces;

public interface IEmployeeService
{
    Task<ServiceResult<EmployeeResponseDto>> CrearEmpleadoAsync(CreateEmployeeRequestDto request);
}
