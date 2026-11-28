// ============================================================================
// ARCHIVO: Services/Interfaces/IAuthService.cs
// PROPÓSITO: Contrato del servicio de autenticación (registro autónomo de
//            clientes e inicio de sesión con emisión de JWT y control de bloqueo).
// ============================================================================

using APIWeb.DTOs.Auth;

namespace APIWeb.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponseDto>> RegistrarAsync(RegisterRequestDto request);
        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    }
}
