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
