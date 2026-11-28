// ============================================================================
// ARCHIVO: DTOs/Employees/EmployeeResponseDto.cs
// PROPÓSITO: DTO de salida al registrar un empleado/administrador.
//            Devuelve los IDs generados en ambas tablas (EmployeeId en 'employees'
//            y UserId en 'usuarios') sin exponer el PasswordHash.
// ============================================================================

namespace APIWeb.DTOs.Employees;

public class EmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
