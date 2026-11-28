// ============================================================================
// ARCHIVO: Models/Usuario.cs
// PROPÓSITO: Entidad que representa la tabla 'usuarios' en MySQL (nwind_pos).
//            DE DÓNDE VIENE: Tabla extendida mediante el script 001_extend_nwind_pos.sql.
//            POR QUÉ EXISTE: Separa las credenciales de acceso y seguridad (Email,
//            PasswordHash, Rol, bloqueo por 3 intentos fallidos) de los datos
//            comerciales de Northwind, vinculándose opcionalmente a:
//            - CustomerId (si el usuario es un cliente en la tabla 'customers').
//            - EmployeeId (si el usuario es un empleado/admin en la tabla 'employees').
// ============================================================================

using APIWeb.Models.Enums;

namespace APIWeb.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Contraseña cifrada con PBKDF2 (PasswordHasher de ASP.NET Core Identity). Nunca se guarda en texto plano.
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Rol { get; set; } = UserRole.Customer;

    // Contador de intentos fallidos de login; al llegar a 3 se activa LockoutEnd por 15 minutos.
    public uint FailedLoginAttempts { get; set; }

    // Fecha/hora UTC hasta la cual la cuenta permanece bloqueada tras 3 intentos fallidos.
    public DateTime? LockoutEnd { get; set; }

    // Estado de la cuenta; se pone en false si el cliente asociado recibe baja lógica.
    public bool IsActive { get; set; } = true;

    // FK hacia la tabla 'customers' (cuando Rol == Customer)
    public string? CustomerId { get; set; }

    // FK hacia la tabla 'employees' (cuando Rol == Employee o Admin)
    public int? EmployeeId { get; set; }

    public DateTime UltimoAcceso { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }
}
