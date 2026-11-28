// ============================================================================
// ARCHIVO: Services/AuthService.cs
// PROPÓSITO: Lógica de Autenticación, Registro Autónomo de Clientes, Bloqueo
//            tras 3 intentos fallidos y generación de Tokens JWT.
//            AQUÍ SE HACE:
//            1. RegistrarAsync: Crea de forma atómica (transacción MySQL) el
//               registro en la tabla 'customers' y la cuenta en 'usuarios' con
//               Rol = Customer y contraseña hasheada.
//            2. LoginAsync: Verifica credenciales, controla el bloqueo de 15 min
//               al acumular 3 intentos fallidos (HTTP 423 Locked) y emite el JWT.
//            3. BuildAuthResponse: Construye y firma el JWT incluyendo los Claims
//               de Id, Nombre, Email, Rol, CustomerId y EmployeeId.
// ============================================================================

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

    // ------------------------------------------------------------------------
    // REGISTRO AUTÓNOMO DE CLIENTES (POST /api/auth/register)
    // DE DÓNDE VIENEN LOS DATOS: Del formulario register.html -> register.js.
    // PROCESO PASO A PASO:
    // 1. Sanitiza correo y nombre con .Trim() y pasa el correo a minúsculas.
    // 2. Verifica en la tabla 'usuarios' que el correo no exista (evita duplicados -> 409).
    // 3. Abre una transacción en MySQL para insertar en 'customers' y 'usuarios':
    //    si algo falla, hace Rollback para no dejar registros huérfanos.
    // 4. Genera un CustomerId único de 5 caracteres alfanuméricos.
    // 5. Hashea la contraseña con PasswordHasher (PBKDF2) antes de guardar.
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<AuthResponseDto>> RegistrarAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var nombre = request.Nombre.Trim();

        // REGLA DE NEGOCIO (Evitar duplicados): valida que el correo no esté registrado
        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Email == email);
        if (existeCorreo)
            return ServiceResult<AuthResponseDto>.Fail(409, "El correo electrónico ya está registrado.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customerId = await GenerateUniqueCustomerIdAsync();
            var now = DateTime.UtcNow;

            // 1) Crea su perfil comercial en la tabla 'customers' de Northwind
            var customer = new Customer
            {
                CustomerId = customerId,
                CompanyName = nombre.Length > 40 ? nombre[..40] : nombre,
                ContactName = nombre.Length > 30 ? nombre[..30] : nombre,
                IsDeleted = false
            };

            // 2) Crea su cuenta de acceso en la tabla 'usuarios' vinculada por CustomerId
            var usuario = new Usuario
            {
                Nombre = nombre,
                Email = email,
                Rol = UserRole.Customer, // Siempre Customer en el registro público
                FailedLoginAttempts = 0,
                IsActive = true,
                CustomerId = customerId,
                FechaRegistro = now,
                UltimoAcceso = now
            };

            // Cifrado unidireccional de la contraseña (nunca se almacena en texto plano)
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

    // ------------------------------------------------------------------------
    // INICIO DE SESIÓN Y BLOQUEO POR 3 INTENTOS FALLIDOS (POST /api/auth/login)
    // DE DÓNDE VIENEN LOS DATOS: Del formulario login.html -> login.js.
    // PROCESO PASO A PASO:
    // 1. Busca el usuario por Email en la tabla 'usuarios' y verifica que IsActive == true.
    // 2. AQUÍ SE VALIDA EL BLOQUEO DE CUENTA:
    //    - Si LockoutEnd > DateTime.UtcNow, rechaza el acceso con HTTP 423 (Locked)
    //      indicando cuántos minutos faltan para desbloquearse.
    //    - Si el tiempo de bloqueo ya venció, reinicia FailedLoginAttempts = 0.
    // 3. Verifica el hash de la contraseña:
    //    - Si falla: incrementa FailedLoginAttempts++. Al llegar a 3 intentos,
    //      asigna LockoutEnd = now.AddMinutes(15) y devuelve HTTP 423.
    //    - Si acierta: limpia el contador de fallos, actualiza UltimoAcceso y emite el JWT.
    // ------------------------------------------------------------------------
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        // Si el correo no existe o la cuenta fue desactivada (por ejemplo, baja lógica del cliente)
        if (usuario is null || !usuario.IsActive)
            return ServiceResult<AuthResponseDto>.Fail(401, "Credenciales inválidas.");

        var now = DateTime.UtcNow;

        // Revisión de bloqueo activo por fuerza bruta (3 intentos fallidos previos)
        if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > now)
        {
            var minutosRestantes = Math.Ceiling((usuario.LockoutEnd.Value - now).TotalMinutes);
            return ServiceResult<AuthResponseDto>.Fail(
                423,
                $"Cuenta bloqueada temporalmente por múltiples intentos fallidos. Inténtalo nuevamente en {minutosRestantes} minuto(s).");
        }
        else if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value <= now)
        {
            // El periodo de 15 minutos ya transcurrió; se desbloquea automáticamente
            usuario.FailedLoginAttempts = 0;
            usuario.LockoutEnd = null;
        }

        // Compara la contraseña ingresada contra el PasswordHash guardado en MySQL
        var verifyResult = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            usuario.FailedLoginAttempts++;

            // AQUÍ SE APLICA LA REGLA DE LOS 3 INTENTOS DE ACCESO:
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

        // Login exitoso: reinicia contadores de bloqueo y registra fecha de último acceso
        usuario.FailedLoginAttempts = 0;
        usuario.LockoutEnd = null;
        usuario.UltimoAcceso = now;
        await _context.SaveChangesAsync();

        var authResponse = BuildAuthResponse(usuario);
        return ServiceResult<AuthResponseDto>.Ok(authResponse, 200, "Inicio de sesión exitoso.");
    }

    // ------------------------------------------------------------------------
    // GENERACIÓN DEL TOKEN JWT
    // DE DÓNDE TOMA LA CONFIGURACIÓN: De appsettings.json ("Jwt:Key", "Issuer", "Audience", "ExpireMinutes").
    // QUÉ GUARDA DENTRO DEL TOKEN (Claims):
    // - Sub / NameIdentifier: Id del usuario en la tabla 'usuarios'.
    // - Name / Email / Role: Nombre, correo y rol ("Customer", "Employee" o "Admin").
    // - CustomerId: ID del cliente en 'customers' (usado por OrdersController para filtrar sus pedidos).
    // - EmployeeId: ID del empleado en 'employees' (usado por OrdersController para registrar quién vendió).
    // ------------------------------------------------------------------------
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

        // Si es cliente, inyecta su CustomerId en el JWT
        if (!string.IsNullOrWhiteSpace(usuario.CustomerId))
            claims.Add(new Claim("CustomerId", usuario.CustomerId));

        // Si es empleado o administrador vinculado a 'employees', inyecta su EmployeeId en el JWT
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

    // Genera un código aleatorio de 5 caracteres (A-Z, 0-9) y verifica en MySQL
    // que no choque con ningún CustomerId existente en la tabla 'customers'.
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
