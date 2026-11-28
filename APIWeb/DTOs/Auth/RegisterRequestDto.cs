// ============================================================================
// ARCHIVO: DTOs/Auth/RegisterRequestDto.cs
// PROPÓSITO: DTO de entrada para el registro autónomo de clientes
//            (POST /api/auth/register recibido desde register.js).
//            POR QUÉ NO RECIBE "ROL":
//            Para evitar escalación de privilegios; AuthService fuerza siempre
//            que cualquier registro público quede con Rol = UserRole.Customer.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
