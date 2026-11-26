import { clearSession, getToken } from "./session.js";


const API_BASE_URL =
    window.location.port === "5500"
        ? "https://localhost:7214/api"
        : "/api";

export class ApiError extends Error {
    constructor(status, message, errors = []) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.errors = errors;
    }
}

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

export async function apiRequest(endpoint, options = {}) {
    const {
        method = "GET",
        body = null,
        auth = true,
        skipAuthRedirect = false,
        headers = {}
    } = options;

    const finalHeaders = {
        Accept: "application/json",
        ...headers
    };

    if (body !== null && !(body instanceof FormData)) {
        finalHeaders["Content-Type"] = "application/json";
    }

    if (auth) {
        const token = getToken();
        if (token) {
            finalHeaders["Authorization"] = `Bearer ${token}`;
        }
    }

    let response;
    try {
        response = await fetch(`${API_BASE_URL}${endpoint}`, {
            method,
            headers: finalHeaders,
            body: body === null ? null : body instanceof FormData ? body : JSON.stringify(body)
        });
    } catch {
        throw new ApiError(0, "No fue posible conectar con el servidor. Verifica que la API esté en ejecución.");
    }

    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get("content-type") || "";
    const isJson = contentType.includes("application/json") || contentType.includes("application/problem+json");
    const data = isJson ? await response.json().catch(() => null) : null;

    if (!response.ok) {
        const errors = normalizeErrors(data);
        const message =
            data?.message ||
            data?.title ||
            (errors.length > 0 ? errors[0] : "Ocurrió un error al procesar la solicitud.");

        if (response.status === 401 && !skipAuthRedirect) {
            clearSession();
            window.location.replace("login.html?reason=session");
            throw new ApiError(401, "Tu sesión expiró. Inicia sesión nuevamente.", errors);
        }

        if (response.status === 403 && !skipAuthRedirect) {
            window.location.replace("acceso-denegado.html");
            throw new ApiError(403, "No tienes permisos para acceder a este recurso.", errors);
        }

        throw new ApiError(response.status, message, errors);
    }

    return data;
}

export const apiClient = {
    get: (endpoint, options = {}) => apiRequest(endpoint, { ...options, method: "GET" }),
    post: (endpoint, body, options = {}) => apiRequest(endpoint, { ...options, method: "POST", body }),
    put: (endpoint, body, options = {}) => apiRequest(endpoint, { ...options, method: "PUT", body }),
    patch: (endpoint, body, options = {}) => apiRequest(endpoint, { ...options, method: "PATCH", body }),
    delete: (endpoint, options = {}) => apiRequest(endpoint, { ...options, method: "DELETE" })
};