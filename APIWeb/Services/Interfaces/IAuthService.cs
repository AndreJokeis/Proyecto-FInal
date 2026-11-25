using APIWeb.DTOs.Auth;

namespace APIWeb.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthResponseDto>> RegistrarAsync(RegisterRequestDto request);
        Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    }
}
