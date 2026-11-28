// ============================================================================
// ARCHIVO: DTOs/Employees/CreateEmployeeRequestDto.cs
// PROPÓSITO: DTO de entrada para POST /api/admin/employees (enviado desde empleados.js).
//            Recibe tanto los datos para la tabla 'employees' (FirstName, LastName, Title)
//            como las credenciales para la tabla 'usuarios' (Role, Email, Password).
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Employees;

public class CreateEmployeeRequestDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(10, ErrorMessage = "El nombre no puede exceder los 10 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(20, ErrorMessage = "El apellido no puede exceder los 20 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "El puesto no puede exceder los 30 caracteres.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(150, ErrorMessage = "El correo electrónico es demasiado largo.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [StringLength(100, ErrorMessage = "La contraseña no puede exceder los 100 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
