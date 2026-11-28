// ============================================================================
// ARCHIVO: Models/Employee.cs
// PROPÓSITO: Entidad de la tabla 'employees' de Northwind en MySQL.
//            CUÁNDO SE USA:
//            1. Cuando el Administrador da de alta un empleado o admin en empleados.html
//               (EmployeeService inserta aquí primero para obtener EmployeeId y luego
//               crea su usuario en la tabla 'usuarios' con ese EmployeeId).
//            2. En el historial y detalle de ventas (OrderService hace Include(o => o.Employee)
//               para tomar FirstName + LastName y mostrar quién realizó la venta).
// ============================================================================

namespace APIWeb.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Title { get; set; }

    public string? TitleOfCourtesy { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime? HireDate { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? HomePhone { get; set; }

    public string? Extension { get; set; }

    public byte[]? Photo { get; set; }

    public string? Notes { get; set; }

    public int? ReportsTo { get; set; }

    public virtual ICollection<Employee> InverseReportsToNavigation { get; set; } = new List<Employee>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Employee? ReportsToNavigation { get; set; }
}
