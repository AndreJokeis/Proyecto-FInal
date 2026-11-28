// ============================================================================
// ARCHIVO: Controllers/AuthController.cs
// PROPÓSITO: Endpoints públicos de autenticación (/api/auth/register y /api/auth/login).
//            POR QUÉ TIENEN [AllowAnonymous]:
//            Porque el usuario todavía no tiene un token JWT cuando apenas va a
//            crear su cuenta de cliente o a iniciar sesión.
// ============================================================================

using APIWeb.DTOs.Auth;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register -> Registro autónomo de clientes (llamado desde register.js)
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegistrarAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new
            {
                success = false,
                message = result.Message,
                errors = Array.Empty<string>()
            });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    // POST /api/auth/login -> Inicio de sesión y emisión de JWT (llamado desde login.js)
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new
            {
                success = false,
                message = result.Message,
                errors = Array.Empty<string>()
            });
        }

        return Ok(result.Data);
    }
}
