# PROJECT MEMORY & DECISION LOG

## Propósito del Proyecto
Sistema web de Punto de Venta (POS) con arquitectura desacoplada (Backend REST + Frontend web) para la materia de **Programación Web II** (ITSUR, 7.º Semestre). Implementa operaciones CRUD completas sobre la base de datos **Northwind (`nwind`) en MySQL**, gestión de usuarios y roles, reglas estrictas de integridad comercial, controles de seguridad web y despliegue productivo en una Máquina Virtual de Azure con Windows mediante IIS / IIS Express.

## Dominio, Roles y Reglas de Seguridad Acordadas
- **Base de datos base (`nwind` - MySQL 8.x)**:
  - Script de origen: `Documentation/nwind_completa.sql`.
  - Tablas principales de Northwind en uso: `customers`, `employees`, `products`, `categories`, `suppliers`, `shippers`, `orders`, `order details`.
  - Extensiones incrementales acordadas (mediante script SQL versionado sin destruir datos precargados):
    * Tabla de cuentas/autenticación (`users`) con credenciales hasheadas, rol asignado, contador de intentos fallidos (`FailedLoginAttempts`), fecha de bloqueo (`LockoutEnd`), estado activo (`IsActive`) y vínculo opcional a `CustomerID` o `EmployeeID`.
    * Soporte de eliminación lógica (`IsDeleted` / `IsActive`) en `customers` (y en entidades administrativas que requieran baja lógica); `products` ya dispone del campo `Discontinued`.
- **Matriz de Roles (RBAC)**:
  - `Customer` (Cliente): Registro autónomo desde la web (crea usuario y su registro correspondiente en `customers`), consulta de catálogo activo, registro de compras propias y consulta de sus pedidos.
  - `Employee` (Empleado): Alta exclusiva por un `Admin` (crea usuario y su registro correspondiente en `employees`), operación de punto de venta, gestión de catálogo y gestión de clientes (sin permisos para administrar cuentas de empleados o roles privilegiados).
  - `Admin` (Administrador): Control total del sistema, alta y administración de empleados/administradores, gestión de catálogo, gestión de clientes (incluyendo eliminación lógica) y supervisión general de ventas.
- **Reglas de Negocio e Integridad Innegociables**:
  - **Cero manipulación de precios**: El servidor obtiene `UnitPrice` directamente de la tabla `products`; cualquier precio o total enviado por el frontend se ignora.
  - **Productos descontinuados y stock**: Se bloquea la compra de productos con `Discontinued = 1` o cantidad superior a `UnitsInStock` (o cantidades <= 0).
  - **Prevención de duplicados**: Correo/usuario único en registro, `CustomerID` único y prevención de líneas duplicadas del mismo producto en `order details`.
  - **Eliminación lógica**: Los clientes nunca se eliminan físicamente para preservar la integridad histórica de `orders`.
- **Controles de Seguridad Obligatorios**:
  - Autenticación con JWT + autorización en cada endpoint protegido + verificación de acceso por URL en el Frontend.
  - Protección contra fuerza bruta: Captcha y/o bloqueo temporal tras 3 intentos fallidos de inicio de sesión.
  - Anti-Clickjacking mediante cabeceras `X-Frame-Options: DENY` y `Content-Security-Policy: frame-ancestors 'none'`.
  - Sanitización de entradas y escapado de salidas para mitigación de XSS.
  - Respuestas de API mínimas mediante DTOs y mensajes de error adecuados sin fuga de información técnica.

## Bitácora de Decisiones Técnicas
- **2026-11-22**: Definición oficial del equipo de 4 integrantes: Joseph (Backend), Richy (Backend & QA), Jesús (Frontend) y Luis (Frontend), apoyados por Asistente IA como Lead Tech & PM.
- **2026-11-22**: Definición del stack tecnológico inmutable: ASP.NET Core Web API (.NET 8) + JWT + Entity Framework Core (`Pomelo.EntityFrameworkCore.MySql`) + Frontend en HTML5, Bootstrap 5 y JavaScript modular + despliegue en Azure VM Windows (IIS / IIS Express).
- **2026-11-22**: Se acuerda mantener la documentación viva de gobierno técnico (`agents.md`, `memory.md` y `skills.md`) dentro de `Documentation/` junto a `EXPECTS.md` y `nwind_completa.sql`.
- **2026-11-22**: Flujo de versionamiento Git: rama `main` estable y rama `develop` de integración; cada tarea se desarrolla en ramas `feature/`, `fix/` o `chore/` integradas vía Pull Request.
- **2026-11-22**: Se establece la política de inmutabilidad de `memory.md` para conservar trazabilidad completa de decisiones y avances entre los 4 integrantes.
- **2026-11-22**: Se actualiza el stack técnico para utilizar ASP.NET Core Web API sobre **.NET 10** y Entity Framework Core con el proveedor **`MySql.EntityFrameworkCore`**, en concordancia con el proyecto base actual.
- **2026-11-22**: Se retira la integración con Google OAuth. La autenticación se implementará posteriormente con cuentas propias, contraseñas hasheadas, roles y JWT.
- **2026-11-22**: Se crea la base de trabajo `nwind_pos` como copia de `nwind`, conservando `nwind` como referencia original sin modificaciones.
- **2026-11-22**: Se amplía `nwind_pos` con los campos de autenticación en `usuarios` (`Rol`, `FailedLoginAttempts`, `LockoutEnd`, `IsActive`, `CustomerID` y `EmployeeID`) y con `IsDeleted` en `customers`.
- **2026-11-22**: Se ajustan las relaciones históricas de `nwind_pos` para evitar el borrado en cascada de clientes con órdenes y de productos con detalles de órdenes.
- **2026-11-22**: Se sincronizan los modelos de Entity Framework con `nwind_pos`, creando `Usuario`, `UserRole`, las relaciones de cuenta y la propiedad `Customer.IsDeleted`. La API utiliza `nwind_pos` mediante `appsettings.json`.
- **2026-11-22**: Para esta tarea puntual, el usuario implementó personalmente los DTOs de autenticación con acompañamiento del Asistente IA; esta decisión no constituye una regla general para el equipo ni para futuras tareas.
- **2026-11-22**: Se crean y validan `RegisterRequestDto`, `LoginRequestDto` y `AuthResponseDto` en `APIWeb/DTOs/Auth`. Se corrigió una llave adicional en `RegisterRequestDto` y la solución compiló correctamente.
- **2026-11-25**: Se elimina `WeatherForecast` para cumplir con la política de mínima exposición de la API.
- **2026-11-25**: Se configura la base de seguridad en `Program.cs` y `appsettings.json`: autenticación JWT con roles, `GlobalExceptionMiddleware` para manejo limpio de errores y cabeceras HTTP anti-Clickjacking (`X-Frame-Options` y `Content-Security-Policy`).
- **2026-11-25**: Se acuerda la división modular del Backend para trabajar en paralelo sin conflictos: Richy toma Infraestructura/Seguridad, Auth y Clientes (eliminación lógica); Joseph toma Alta de Empleados, Catálogo de Productos y registro de Ventas (POS), separando los registros de inyección de dependencias en `Program.cs`.

## Hoja de Ruta y Sprints (Backlog Vivo)

### Sprint 0: Base del Proyecto, Esquema MySQL y Arquitectura Inicial
- [x] Crear rama `develop` y estructura base de carpetas (`Backend/`, `Frontend/`, `Database/`).
- [x] Diseñar y versionar script incremental SQL sobre `nwind` para tabla `users` (roles + intentos fallidos/lockout) y campo de eliminación lógica en `customers`. *(Joseph / Richy)*
- [x] Inicializar solución ASP.NET Core Web API (.NET 10) con EF Core (`MySql.EntityFrameworkCore`), configuración de CORS, Swagger (solo en desarrollo) y middleware global de excepciones + cabeceras anti-clickjacking. *(Joseph)*
- [ ] Crear estructura base del Frontend (HTML5, Bootstrap 5, módulo `apiClient.js`, layout compartido y manejo de alertas/toasts). *(Jesús / Luis)*

### Sprint 1: Autenticación, Roles, Lockout/Captcha y Protección de Rutas
- [ ] Implementar registro autónomo de clientes (`POST /api/auth/register`) vinculando cuenta en `users` y registro en `customers` sin duplicados. *(Richy)*
- [ ] Implementar inicio de sesión (`POST /api/auth/login`) con JWT, hash seguro de contraseñas, Captcha y/o bloqueo tras 3 intentos fallidos. *(Richy)*
- [ ] Implementar alta exclusiva de empleados/administradores por `Admin` (`POST /api/admin/employees`) registrando en `users` y `employees`. *(Joseph)*
- [ ] Construir pantallas de Login, Registro de Cliente y Panel de Alta de Empleados + guardas de acceso por URL según rol (`authGuard.js`). *(Luis)*
- [ ] Verificar con pruebas de QA: bloqueo al 3.er intento fallido, acceso denegado (`401`/`403`) en API y redirección por URL. *(Richy)*

### Sprint 2: Catálogo de Productos y Gestión de Clientes (Eliminación Lógica)
- [ ] Implementar endpoints CRUD de productos con DTOs mínimos, validaciones de negocio y manejo de estado `Discontinued`. *(Joseph)*
- [ ] Implementar endpoints CRUD de clientes con eliminación lógica (`IsDeleted`), evitando borrado físico y duplicados. *(Richy)*
- [ ] Construir vistas de Catálogo de Productos y Gestión de Clientes en Bootstrap 5 con validaciones visuales y protección anti-XSS (`textContent`). *(Jesús / Luis)*
- [ ] Pruebas de integración de CRUD, sanitización de inputs y mensajes de error adecuados. *(Richy)*

### Sprint 3: Módulo de Punto de Venta (Registro de Ventas)
- [ ] Implementar registro transaccional de ventas (`POST /api/orders`) insertando en `orders` y `order details`, tomando `UnitPrice` exclusivamente desde BD, bloqueando productos descontinuados, validando stock y descontando inventario. *(Joseph)*
- [ ] Implementar consulta de historial de ventas filtrada por rol (el cliente solo ve sus propias órdenes; `Employee`/`Admin` consultan ventas operativas). *(Richy)*
- [ ] Construir interfaz de Punto de Venta / Carrito y vista de Historial de Ventas con feedback claro al usuario. *(Jesús)*
- [ ] Pruebas de seguridad de negocio: intento de manipular precio desde payload, compra de producto descontinuado, stock insuficiente y duplicado de productos en orden. *(Richy)*

### Sprint 4: Hardening de Seguridad, QA Final y Despliegue en Azure VM
- [ ] Auditoría de endpoints y DTOs: eliminar controladores/endpoints sobrantes y verificar mínima exposición de datos. *(Joseph / Richy)*
- [ ] Auditoría de seguridad web: pruebas de XSS, prueba de Clickjacking con `<iframe>`, verificación de cabeceras HTTP, permisos por URL y bloqueo de login. *(Richy / Luis)*
- [ ] Configurar publicación de la aplicación en Máquina Virtual de Azure (Windows) con IIS / IIS Express y conexión segura a MySQL. *(Todo el equipo)*
- [ ] Smoke tests en entorno publicado y cierre de documentación para revisión final. *(Todo el equipo)*
