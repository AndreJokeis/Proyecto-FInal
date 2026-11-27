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

        if (!Enum.TryParse<UserRole>(roleInput, ignoreCase: true, out var parsedRole) ||
            (parsedRole != UserRole.Employee && parsedRole != UserRole.Admin))
        {
            return ServiceResult<EmployeeResponseDto>.Fail(
                400,
                "El rol especificado no es válido. Solo se permite registrar cuentas de tipo 'Employee' o 'Admin'.");
        }

        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Email == email);
        if (existeCorreo)
            return ServiceResult<EmployeeResponseDto>.Fail(409, "El correo electrónico ya está registrado.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            var employee = new Employee
            {
                FirstName = firstName,
                LastName = lastName,
                Title = title,
                HireDate = now
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var fullName = $"{firstName} {lastName}".Trim();
            var usuario = new Usuario
            {
                Nombre = fullName,
                Email = email,
                Rol = parsedRole,
                FailedLoginAttempts = 0,
                IsActive = true,
                EmployeeId = employee.EmployeeId,
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
