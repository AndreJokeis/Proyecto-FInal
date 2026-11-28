// ============================================================================
// ARCHIVO: Services/ServiceResult.cs
// PROPÓSITO: Clase envoltorio genérica para comunicar resultados desde la capa
//            de Servicios hacia los Controladores sin lanzar excepciones para
//            errores de negocio esperados.
//            CÓMO FUNCIONA:
//            - ServiceResult<T>.Ok(data, statusCode, message) -> operación exitosa (200 / 201).
//            - ServiceResult<T>.Fail(statusCode, message) -> regla de negocio rechazada
//              (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found,
//               409 Conflict por duplicado, 423 Locked por 3 intentos fallidos).
// ============================================================================

namespace APIWeb.Services;

public class ServiceResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data, int statusCode = 200, string message = "Operación exitosa.") =>
        new() { Success = true, StatusCode = statusCode, Message = message, Data = data };

    public static ServiceResult<T> Fail(int statusCode, string message) =>
        new() { Success = false, StatusCode = statusCode, Message = message };
}
