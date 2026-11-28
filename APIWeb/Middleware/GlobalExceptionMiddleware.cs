// ============================================================================
// ARCHIVO: Middleware/GlobalExceptionMiddleware.cs
// PROPÓSITO: Manejo centralizado de errores no controlados (HTTP 500).
//            AQUÍ SE CUMPLEN DOS REQUISITOS:
//            1. "Mensajes adecuados": devuelve al cliente un mensaje amigable en JSON.
//            2. "Mínima exposición de información": evita que salgan detalles técnicos
//               de MySQL, rutas del servidor o trazas de código (Stack Trace) al navegador.
// ============================================================================

using System.Net;
using System.Text.Json;

namespace APIWeb.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // PROCESO: Deja pasar la petición hacia el controlador; si ocurre cualquier
        // excepción inesperada en la BD o servicios, la captura en el bloque catch,
        // la registra en el log interno del servidor y responde con HandleExceptionAsync.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error en la aplicación.");
                await HandleExceptionAsync(context);
            }
        }

        // AQUÍ SE CONSTRUYE LA RESPUESTA ESTÁNDAR DE ERROR 500:
        // Mismo formato JSON { success, message, errors } que consume apiClient.js en el Frontend.
        private static Task HandleExceptionAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                success = false,
                message = "Ocurrió un error interno en el servidor. Intenta nuevamente más tarde.",
                errors = Array.Empty<string>()
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
