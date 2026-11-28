// ============================================================================
// ARCHIVO: Models/Enums/UserRole.cs
// PROPÓSITO: Define los 3 roles oficiales del sistema (RBAC - Control de Acceso Basado en Roles).
//            - Customer: Cliente que se registra de forma autónoma, ve el catálogo,
//                        realiza compras propias y consulta únicamente sus pedidos.
//            - Employee: Empleado dado de alta por un Admin; opera el Punto de Venta,
//                        administra productos y gestiona clientes (incluyendo baja lógica).
//            - Admin:    Administrador con acceso total, incluyendo el módulo exclusivo
//                        de alta de empleados y otros administradores.
// ============================================================================

namespace APIWeb.Models.Enums;

public enum UserRole
{
    Customer,
    Employee,
    Admin
}
