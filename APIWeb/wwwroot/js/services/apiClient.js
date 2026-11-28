// ============================================================================
// ARCHIVO: apiClient.js
// PROPÓSITO: Cliente HTTP centralizado (fetch) para todas las peticiones a la API.
//            Adjunta automáticamente el Token JWT en la cabecera Authorization
//            y maneja errores globales (401 No autenticado, 403 Sin permisos).
// ============================================================================

import { clearSession, getToken } from "./session.js";

// Si se abre con Live Server (puerto 5500) apunta a localhost:7214;
// cuando corre publicado en IIS / ASP.NET Core usa la ruta relativa "/api".
const API_BASE_URL =
    window.location.port === "5500"
        ? "https://localhost:7214/api"
        : "/api";

/**
 * Clase de error personalizada para transportar el código HTTP (status),
 * el mensaje amigable para el usuario y la lista de errores de validación.
 */
export class ApiError extends Error {
    constructor(status, message, errors = []) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.errors = errors;
    }
}

/**
 * Extrae y normaliza los errores de validación que devuelve ASP.NET Core
 * (por ejemplo, DataAnnotations de los DTOs) en un arreglo simple de textos.
 */
function normalizeErrors(payload) {
    if (!payload) return [];
    if (Array.isArray(payload.errors)) {
        return payload.errors.map(String);
    }
    if (payload.errors && typeof payload.errors === "object") {
        return Object.values(payload.errors).flat().map(String);
    }
    return [];
}

/**
 * FUNCIÓN PRINCIPAL DE PETICIONES A LA API:
 * Construye y envía la petición HTTP con fetch(), inyecta el JWT y valida la respuesta.
 */
export async function apiRequest(endpoint, options = {}) {
    const {
        method = "GET",
        body = null,
        auth = true,              // Por defecto todas las peticiones envían el token JWT
        skipAuthRedirect = false, // En login/registro se pone en true para mostrar alerta en la misma pantalla
        headers = {}
    } = options;

    const finalHeaders = {
        Accept: "application/json",
        ...headers
    };

    // Si enviamos un objeto en body, indicamos que viaja en formato JSON
    if (body !== null && !(body instanceof FormData)) {
        finalHeaders["Content-Type"] = "application/json";
    }

    // AQUÍ SE ENVÍA EL TOKEN JWT A LA API:
    // Lee el token de session.js y lo agrega en el header estándar "Authorization: Bearer <token>"
    if (auth) {
        const token = getToken();
        if (token) {
            finalHeaders["Authorization"] = `Bearer ${token}`;
        }
    }

    let response;
    try {
        // Ejecuta la llamada HTTP hacia el controlador correspondiente del Backend
        response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method,
            headers: finalHeaders,
            body: body === null ? null : body instanceof FormData ? body : JSON.stringify(body)
        });
    } catch {
        // Mensaje adecuado si el servidor está apagado o no hay red
        throw new ApiError(0, "No fue posible conectar con el servidor. Verifica que la API esté en ejecución.");
    }

    if (response.status === 204) {
        return null;
    }

    // Verifica si la respuesta del servidor viene en formato JSON
    const contentType = response.headers.get("content-type") || "";
    const isJson = contentType.includes("application/json") || contentType.includes("application/problem+json");
    const data = isJson ? await response.json().catch(() => null) : null;

    // Manejo de respuestas con error HTTP (400, 401, 403, 404, 409, 423, 500)
    if (!response.ok) {
        const errors = normalizeErrors(data);
        const message =
            data?.message ||
            data?.title ||
            (errors.length > 0 ? errors[0] : "Ocurrió un error al procesar la solicitud.");

        // Si el Backend devuelve 401 (token vencido o inválido), cierra sesión y redirige a Login
        if (response.status === 401 && !skipAuthRedirect) {
            clearSession();
            window.location.replace("login.html?reason=session");
            throw new ApiError(401, "Tu sesión expiró. Inicia sesión nuevamente.", errors);
        }

        // Si el Backend devuelve 403 (su rol no tiene permiso en ese endpoint), redirige a Acceso Denegado
        if (response.status === 403 && !skipAuthRedirect) {
            window.location.replace("acceso-denegado.html");
            throw new ApiError(403, "No tienes permisos para acceder a este recurso.", errors);
        }

        throw new ApiError(response.status, message, errors);
    }

    return data;
}

// Objeto exportado con métodos cortos para usar en las páginas (get, post, put, delete)
export const apiClient = {
    get: (endpoint, options = {}) => apiRequest(endpoint, { ...options, method: "GET" }),
    post: (endpoint, body, options = {}) => apiRequest(endpoint, { ...options, method: "POST", body }),
    put: (endpoint, body, options = {}) => apiRequest(endpoint, { ...options, method: "PUT", body }),
    delete: (endpoint, options = {}) => apiRequest(endpoint, { ...options, method: "DELETE" })
};
