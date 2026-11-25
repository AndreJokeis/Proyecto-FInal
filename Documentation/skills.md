# TEAM SKILLS & CODING CONVENTIONS

## 1. Convención de Nombres de Ramas y Commits (Git)
- **Ramas base**:
  - `main`: Código estable y listo para entrega/despliegue. Prohibido hacer push directo.
  - `develop`: Rama de integración continua del equipo.
- **Ramas de trabajo** (siempre salen de `develop` y regresan por Pull Request):
  - Funcionalidades: `feature/nombre-corto-en-ingles` (ej. `feature/jwt-auth-lockout`, `feature/pos-checkout-ui`)
  - Correcciones: `fix/descripcion-del-bug` (ej. `fix/discontinued-product-validation`)
  - Configuración / Infraestructura: `chore/descripcion` (ej. `chore/mysql-efcore-setup`)
  - Documentación: `docs/descripcion` (ej. `docs/update-memory-sprint1`)
- **Prefijos de commits**: `feat:`, `fix:`, `chore:`, `refactor:`, `test:`, `docs:` (mensajes breves y claros).

## 2. Arquitectura por Capas y Patrón DTO (ASP.NET Core Web API)
Cada módulo sigue estrictamente el flujo `Controller` → `Service` → `DbContext / Repository`.
- **Regla de DTOs**: Nunca recibir ni devolver entidades de EF Core directamente en un `Controller`. Definir `RequestDto` y `ResponseDto` con solo los campos necesarios:
```csharp
// Entrada para crear un ítem de venta: NUNCA incluir UnitPrice ni Total
public sealed record CreateOrderItemDto(
    int ProductId,
    short Quantity
);

// Salida de producto: únicamente lo que necesita el cliente
public sealed record ProductResponseDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    short UnitsInStock,
    bool Discontinued
);
```
- **Validaciones**: Usar `DataAnnotations` (`[Required]`, `[Range]`, `[EmailAddress]`, `[StringLength]`) en los DTOs de entrada y validaciones de reglas de negocio dentro de los `Services`.

## 3. Estándar de Respuestas HTTP y Mensajes Adecuados
Todas las respuestas de error deben ser claras para el usuario final y nunca exponer excepciones internas, rutas del servidor ni consultas SQL.
- **Códigos HTTP obligatorios**:
  - `200 OK` / `201 Created`: Operación o creación exitosa.
  - `400 Bad Request`: Datos inválidos o regla de negocio incumplida (ej. producto descontinuado, stock insuficiente).
  - `401 Unauthorized`: Credenciales inválidas o token JWT ausente/expirado.
  - `403 Forbidden`: Usuario autenticado sin rol o permiso para el recurso.
  - `404 Not Found`: Recurso inexistente o dado de baja lógicamente.
  - `409 Conflict`: Registro duplicado (ej. correo o `CustomerID` ya registrado).
  - `423 Locked` / `429 Too Many Requests`: Cuenta bloqueada temporalmente tras 3 intentos fallidos.
  - `500 Internal Server Error`: Manejado por un `GlobalExceptionMiddleware` que devuelve un mensaje genérico amigable y registra el detalle únicamente en logs del servidor.
```json
{
  "success": false,
  "message": "No es posible completar la venta: el producto seleccionado está descontinuado.",
  "errors": []
}
```

## 4. Patrón de Seguridad Backend (JWT, RBAC, Lockout, Clickjacking y XSS)
- **Autorización estricta por rol**:
  - Proteger cada controlador o acción con `[Authorize]` y roles explícitos: `[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "Admin,Employee")]`, `[Authorize(Roles = "Customer,Employee,Admin")]`.
  - Al consultar datos propios (como pedidos de un cliente), validar siempre el `CustomerID` desde los **claims del JWT**, nunca confiar en un `customerId` enviado por query string o body.
- **Bloqueo por 3 intentos fallidos y/o Captcha**:
  - En cada intento fallido de login, incrementar `FailedLoginAttempts`. Al llegar a `3`, fijar `LockoutEnd = DateTime.UtcNow.AddMinutes(15)` (o exigir validación Captcha) y reiniciar el contador a `0` al autenticarse correctamente.
- **Middleware de cabeceras anti-Clickjacking y hardening** (registrado en `Program.cs`):
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none';";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
```
- **Sanitización de entradas**: Recortar espacios (`Trim()`), validar formatos estrictos y sanitizar cualquier texto libre antes de persistir para prevenir XSS almacenado.

## 5. Patrón de Reglas de Negocio y Persistencia (EF Core + `MySql.EntityFrameworkCore` + MySQL Northwind)
- **Registro de Ventas Transaccional**:
  - Envolver el alta de `orders`, `order details` y actualización de `products.UnitsInStock` dentro de `await using var tx = await _context.Database.BeginTransactionAsync();`.
  - Consultar cada producto en BD por `ProductId`: verificar existencia, `!product.Discontinued` y `product.UnitsInStock >= item.Quantity`.
  - Asignar `orderDetail.UnitPrice = product.UnitPrice ?? 0m` directamente desde la entidad consultada en MySQL.
  - Agrupar o rechazar `ProductId` duplicados dentro de la misma solicitud de venta.
- **Eliminación Lógica (Soft Delete)**:
  - En clientes (`customers`), la acción de eliminar ejecuta `customer.IsDeleted = true` y guarda cambios; jamás invocar `_context.Customers.Remove(customer)`.
  - Aplicar filtro `!c.IsDeleted` en consultas (preferentemente mediante `HasQueryFilter` en `OnModelCreating`) para evitar fugas de registros eliminados.

## 6. Convenciones de Frontend (HTML5, Bootstrap 5 y JS Modular)
- **Cliente HTTP centralizado (`js/services/apiClient.js`)**:
  - Todas las llamadas `fetch` pasan por una función central que adjunta el header `Authorization: Bearer <token>`, maneja respuestas JSON de error y redirige a `login.html` en `401` o muestra vista de acceso denegado en `403`.
- **Protección de Acceso por URL (`js/guards/authGuard.js`)**:
  - Cada página protegida declara los roles permitidos y ejecuta `requireRoles(['Admin'])` (o los roles que correspondan) al cargar el documento antes de renderizar información sensible.
  - Ocultar opciones del menú en UI según el rol del usuario (recordando que la seguridad real siempre se refuerza en la API).
- **Prevención estricta de XSS en el DOM**:
  - **PROHIBIDO** insertar valores provenientes de la API o inputs de usuario con `element.innerHTML = ...`.
  - Usar siempre `document.createElement(...)` y `element.textContent = value` al construir filas de tablas, tarjetas o mensajes.
- **Componentes Bootstrap 5 recomendados**:
  - Formularios con validación visual (`needs-validation`, `is-invalid`, `invalid-feedback`).
  - Mensajes de feedback claros con `Toast` o `Alert` de Bootstrap (nunca usar `alert()` nativo del navegador).
  - Modales de confirmación (`Modal`) antes de realizar bajas lógicas de clientes o confirmar una venta.

## 7. Checklist de QA y Seguridad (Obligatorio antes de Merge a `main` y Despliegue)
- [ ] **Control de acceso**: Un `Customer` recibe `403` si intenta llamar endpoints de `Admin` o `Employee`, y es redirigido si abre la URL administrativa directamente.
- [ ] **Integridad de ventas**: Enviar un `UnitPrice` alterado en el JSON de compra es ignorado y se cobra el precio real de MySQL.
- [ ] **Productos descontinuados y stock**: Intentar comprar un producto con `Discontinued = 1` o cantidad mayor al stock devuelve `400` con mensaje claro.
- [ ] **Eliminación lógica**: Al eliminar un cliente desaparece de los listados activos, pero permanece en MySQL conservando sus `orders` intactas.
- [ ] **Fuerza bruta**: Tras 3 contraseñas incorrectas, la cuenta se bloquea temporalmente y/o exige Captcha con mensaje adecuado.
- [ ] **XSS**: Intentar registrar `<script>alert('xss')</script>` en nombre o dirección no ejecuta código ni al guardar ni al listar.
- [ ] **Clickjacking**: Intentar incrustar cualquier vista de la app dentro de un `<iframe>` externo es bloqueado por las cabeceras HTTP.
- [ ] **Mínima exposición**: Ningún endpoint retorna contraseñas, hashes ni campos ajenos al contrato DTO, y no existen controladores de prueba (ej. `WeatherForecastController`).
- [ ] **Despliegue Azure**: La aplicación corre correctamente bajo IIS / IIS Express en la VM Windows de Azure conectada a MySQL.
