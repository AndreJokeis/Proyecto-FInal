// ============================================================================
// ARCHIVO: Program.cs
// PROPÓSITO: Punto de entrada y configuración general del Backend (ASP.NET Core).
//            AQUÍ SE CONFIGURA:
//            1. Conexión a la base de datos MySQL (nwind_pos) con Entity Framework Core.
//            2. Autenticación mediante Tokens JWT (JSON Web Token) y validación de firma/expiración.
//            3. Autorización basada en roles (Customer, Employee, Admin).
//            4. Inyección de dependencias de los servicios de negocio.
//            5. Seguridad HTTP contra Clickjacking (X-Frame-Options y Content-Security-Policy).
//            6. Servidor de archivos estáticos (carpeta wwwroot donde vive el Frontend).
// ============================================================================

using System.Text;
using APIWeb.Data;
using APIWeb.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using APIWeb.Services;
using APIWeb.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// 1. CONEXIÓN A LA BASE DE DATOS MYSQL
// DE DÓNDE SE TOMA: Lee la cadena "ConexionMySQL" desde appsettings.json.
// POR QUÉ: Conecta Entity Framework Core (ContextoBD) con la base de datos
//          nwind_pos en MySQL para consultar y guardar todas las tablas.
// ----------------------------------------------------------------------------
builder.Services.AddDbContext<ContextoBD>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("ConexionMySQL")!));

// ----------------------------------------------------------------------------
// 2. AUTENTICACIÓN JWT Y AUTORIZACIÓN POR ROLES
// DE DÓNDE SE TOMA: Lee la sección "Jwt" (Key, Issuer, Audience) de appsettings.json.
// PROCESO: Cada vez que el Frontend envía una petición con la cabecera
//          "Authorization: Bearer <token>", ASP.NET Core verifica aquí que:
//          - El token haya sido emitido por nuestro servidor (ValidIssuer).
//          - Esté dirigido a nuestros clientes (ValidAudience).
//          - No haya caducado (ValidateLifetime + ClockSkew = TimeSpan.Zero).
//          - La firma criptográfica HMAC-SHA256 sea auténtica (IssuerSigningKey).
// ----------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Sin margen extra: expira exactamente al cumplirse los 120 min
    };
});

// Habilita la revisión de atributos [Authorize(Roles = "...")] en los controladores
builder.Services.AddAuthorization();

// ----------------------------------------------------------------------------
// 3. INYECCIÓN DE DEPENDENCIAS (SERVICIOS DE NEGOCIO)
// POR QUÉ AddScoped: Crea una instancia del servicio por cada petición HTTP
//                    y la libera al terminar, compartiendo el mismo ContextoBD.
// ----------------------------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();         // Login, registro autónomo de clientes, JWT y bloqueo por 3 intentos
builder.Services.AddScoped<ICustomerService, CustomerService>(); // CRUD de clientes y baja lógica (IsDeleted = true)
builder.Services.AddScoped<IOrderService, OrderService>();       // Punto de venta (POST /api/Orders) e historial de ventas (GET /api/Orders)
builder.Services.AddScoped<IEmployeeService, EmployeeService>(); // Alta exclusiva de empleados/administradores en 'employees' y 'usuarios'
builder.Services.AddScoped<IProductService, ProductService>();   // Catálogo de productos, categorías, proveedores y baja lógica (Discontinued)

// ----------------------------------------------------------------------------
// 4. CONTROLADORES, SWAGGER Y CORS
// POR QUÉ: Registra únicamente los controladores REST necesarios y permite
//          peticiones desde el navegador (incluyendo pruebas locales con Live Server).
// ----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ----------------------------------------------------------------------------
// 5. PIPELINE HTTP (EL ORDEN DE EJECUCIÓN ES CRÍTICO)
// ----------------------------------------------------------------------------

// A) MIDDLEWARE GLOBAL DE EXCEPCIONES:
//    Envuelve toda la aplicación para atrapar cualquier error inesperado (500)
//    y devolver un JSON limpio con un mensaje adecuado sin exponer stack traces.
app.UseMiddleware<GlobalExceptionMiddleware>();

// B) PROTECCIÓN ANTI-CLICKJACKING Y CABECERAS DE SEGURIDAD:
//    AQUÍ SE EVITA EL CLICKJACKING (requisito de seguridad):
//    - X-Frame-Options: DENY -> prohíbe que cualquier página externa incruste
//      nuestra aplicación dentro de un <iframe>.
//    - Content-Security-Policy: frame-ancestors 'none'; -> estándar moderno
//      que bloquea el enmarcado en navegadores actuales.
//    - X-Content-Type-Options: nosniff -> evita que el navegador adivine tipos MIME.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none';";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// C) SWAGGER SOLO EN DESARROLLO (en producción no se expone la documentación interna)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// D) ARCHIVOS ESTÁTICOS DEL FRONTEND (carpeta wwwroot):
//    UseDefaultFiles sirve index.html por defecto y UseStaticFiles entrega
//    los archivos HTML y JS del Frontend desde el mismo servidor IIS / Kestrel.
app.UseDefaultFiles();
app.UseStaticFiles();

// E) AUTENTICACIÓN Y AUTORIZACIÓN:
//    Primero UseAuthentication() lee y valida el token JWT del header Authorization;
//    después UseAuthorization() verifica si el rol del usuario tiene permiso en el endpoint.
app.UseAuthentication();
app.UseAuthorization();

// F) MAPEO DE CONTROLADORES (/api/*)
app.MapControllers();

app.Run();
