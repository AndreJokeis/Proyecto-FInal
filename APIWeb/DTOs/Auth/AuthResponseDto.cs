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
