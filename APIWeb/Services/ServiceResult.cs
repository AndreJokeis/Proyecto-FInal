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