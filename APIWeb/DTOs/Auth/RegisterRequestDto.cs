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
