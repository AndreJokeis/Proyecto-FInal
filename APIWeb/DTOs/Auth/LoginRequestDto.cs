// ============================================================================
// ARCHIVO: DTOs/Auth/LoginRequestDto.cs
// PROPÓSITO: DTO de entrada para POST /api/auth/login (recibido desde login.js).
//            Exige únicamente Email con formato válido y Password obligatoria.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace APIWeb.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}
