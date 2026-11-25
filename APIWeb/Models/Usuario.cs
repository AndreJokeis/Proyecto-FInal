using APIWeb.Models.Enums;

namespace APIWeb.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Rol { get; set; } = UserRole.Customer;

    public uint FailedLoginAttempts { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime UltimoAcceso { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }
}
