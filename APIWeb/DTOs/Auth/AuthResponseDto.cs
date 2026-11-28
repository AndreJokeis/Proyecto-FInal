// ============================================================================
// ARCHIVO: DTOs/Auth/AuthResponseDto.cs
// PROPÓSITO: DTO de salida que devuelve la API al iniciar sesión o registrarse.
//            POR QUÉ SE USA (Mínima exposición):
//            Solo viaja al Frontend el token JWT (AccessToken), su expiración y
//            datos básicos del usuario (UserId, Nombre, Rol). NUNCA viajan el
//            PasswordHash, contador de intentos fallidos ni banderas internas.
// ============================================================================

namespace APIWeb.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public int UserId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }
}
