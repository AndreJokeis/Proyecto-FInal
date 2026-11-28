// ============================================================================
// ARCHIVO: Services/EmployeeService.cs
// PROPÓSITO: Alta exclusiva de Empleados y Administradores.
//            REQUISITO QUE CUMPLE:
//            "Registro de administradores y otros roles con más permisos
//             (empleados solo los da de alta el administrador, registrado también en empleados)".
//            PROCESO PASO A PASO:
//            1. Valida que el rol solicitado sea únicamente 'Employee' o 'Admin'.
//            2. Verifica en la tabla 'usuarios' que el correo no esté duplicado (409 Conflict).
//            3. Abre una transacción en MySQL:
//               a) Inserta primero en la tabla 'employees' de Northwind (FirstName, LastName, Title, HireDate)
//                  y guarda cambios para que MySQL genere el EmployeeId autoincremental.
//               b) Crea la cuenta en la tabla 'usuarios' asignando ese EmployeeId recién generado
//                  y hasheando la contraseña con PasswordHasher.
// ============================================================================

using APIWeb.Data;
using APIWeb.DTOs.Employees;
using APIWeb.Models;
using APIWeb.Models.Enums;
using APIWeb.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ContextoBD _context;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public EmployeeService(ContextoBD context)
    {
        _context = context;
    }

    public async Task<ServiceResult<EmployeeResponseDto>> CrearEmpleadoAsync(CreateEmployeeRequestDto request)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var roleInput = request.Role.Trim();

        // 1. Validación estricta del rol: solo permite crear cuentas Employee o Admin
        if (!Enum.TryParse<UserRole>(roleInput, ignoreCase: true, out var parsedRole) ||
            (parsedRole != UserRole.Employee && parsedRole != UserRole.Admin))
        {
            return ServiceResult<EmployeeResponseDto>.Fail(
                400,
                "El rol especificado no es válido. Solo se permite registrar cuentas de tipo 'Employee' o 'Admin'.");
        }

        // 2. Prevención de duplicados: verifica que el correo no exista ya en 'usuarios'
        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Email == email);
        if (existeCorreo)
            return ServiceResult<EmployeeResponseDto>.Fail(409, "El correo electrónico ya está registrado.");

        // 3. Transacción para asegurar que siempre se creen ambos registros ('employees' + 'usuarios')
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            // PASO A: Insertar en la tabla 'employees' de Northwind
            var employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Title = title,
                HireDate = now
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(); // Aquí MySQL asigna employee.EmployeeId

            // PASO B: Insertar la cuenta en la tabla 'usuarios' vinculada con employee.EmployeeId
            var fullName = $"{firstName} {lastName}".Trim();
            var usuario = new Usuario
            {
                Nombre = fullName,
                Email = email,
                Rol = parsedRole,
                FailedLoginAttempts = 0,
                IsActive = true,
                EmployeeId = employee.EmployeeId, // Vínculo directo con la tabla 'employees'
                FechaRegistro = now,
                UltimoAcceso = now
            };

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new EmployeeResponseDto
            {
                EmployeeId = employee.EmployeeId,
                UserId = usuario.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Title = employee.Title,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            };

            return ServiceResult<EmployeeResponseDto>.Ok(response, 201, "Empleado registrado correctamente.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
