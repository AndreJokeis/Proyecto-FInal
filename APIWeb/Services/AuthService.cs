using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using APIWeb.Data;
using APIWeb.DTOs.Auth;
using APIWeb.Models;
using APIWeb.Models.Enums;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace APIWeb.Services;

public class AuthService : IAuthService
{
    private readonly ContextoBD _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AuthService(ContextoBD context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ServiceResult<AuthResponseDto>> RegistrarAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var nombre = request.Nombre.Trim();

        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Email == email);
        if (existeCorreo)
            return ServiceResult<AuthResponseDto>.Fail(409, "El correo electrónico ya está registrado.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customerId = await GenerateUniqueCustomerIdAsync();
            var now = DateTime.UtcNow;

            var customer = new Customer
            {
                CustomerId = customerId,
                CompanyName = nombre.Length > 40 ? nombre[..40] : nombre,
                ContactName = nombre.Length > 30 ? nombre[..30] : nombre,
                IsDeleted = false
            };

            var usuario = new Usuario
            {
                Nombre = nombre,
                Email = email,
                Rol = UserRole.Customer,
                FailedLoginAttempts = 0,
                IsActive = true,
                CustomerId = customerId,
                FechaRegistro = now,
                UltimoAcceso = now
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);

            _context.Customers.Add(customer);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var authResponse = BuildAuthResponse(usuario);
            return ServiceResult<AuthResponseDto>.Ok(authResponse, 201, "Registro completado correctamente.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !usuario.IsActive)
            return ServiceResult<AuthResponseDto>.Fail(401, "Credenciales inválidas.");

        var now = DateTime.UtcNow;

        if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > now)
        {
            var minutosRestantes = Math.Ceiling((usuario.LockoutEnd.Value - now).TotalMinutes);
            return ServiceResult<AuthResponseDto>.Fail(
                423,
                $"Cuenta bloqueada temporalmente por múltiples intentos fallidos. Inténtalo nuevamente en {minutosRestantes} minuto(s).");
        }
        else if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value <= now)
        {
            usuario.FailedLoginAttempts = 0;
            usuario.LockoutEnd = null;
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            usuario.FailedLoginAttempts++;

            if (usuario.FailedLoginAttempts >= 3)
            {
                usuario.LockoutEnd = now.AddMinutes(15);
                await _context.SaveChangesAsync();
                return ServiceResult<AuthResponseDto>.Fail(
                    423,
                    "Cuenta bloqueada durante 15 minutos tras 3 intentos fallidos de inicio de sesión.");
            }
            

            await _context.SaveChangesAsync();
            return ServiceResult<AuthResponseDto>.Fail(401, "Credenciales inválidas.");
        }

        usuario.FailedLoginAttempts = 0;
        usuario.LockoutEnd = null;
        usuario.UltimoAcceso = now;
        await _context.SaveChangesAsync();

        var authResponse = BuildAuthResponse(usuario);
        return ServiceResult<AuthResponseDto>.Ok(authResponse, 200, "Inicio de sesión exitoso.");
    }

    private AuthResponseDto BuildAuthResponse(Usuario usuario)
    {
        var jwtKey = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var mins) ? mins : 120;

        var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        if (!string.IsNullOrWhiteSpace(usuario.CustomerId))
            claims.Add(new Claim("CustomerId", usuario.CustomerId));

        if (usuario.EmployeeId.HasValue)
            claims.Add(new Claim("EmployeeId", usuario.EmployeeId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = usuario.Id,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol.ToString()
        };
    }

    private async Task<string> GenerateUniqueCustomerIdAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        while (true)
        {
            var id = new string(Enumerable.Range(0, 5)
                .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                .ToArray());

            var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
            if (!exists)
                return id;
        }
    }
}