// ============================================================================
// ARCHIVO: session.js
// PROPÓSITO: Manejo del Token JWT en el navegador (decodificación, guardado en
//            localStorage, lectura de datos/rol del usuario y expiración).
// ============================================================================

// Llaves bajo las cuales se guarda el token y los datos del usuario en localStorage
const TOKEN_KEY = "pos_auth_token";
const USER_KEY = "pos_auth_user";

// Nombres de los Claims estándar de .NET que vienen dentro del payload del JWT
// (generados en el Backend por AuthService.cs -> BuildAuthResponse)
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const NAME_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/name";
const EMAIL_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress";

/**
 * Convierte una cadena codificada en Base64Url (formato que usa JWT)
 * a texto normal UTF-8 legible por JavaScript.
 */
function decodeBase64Url(str) {
    // 1. Reemplaza caracteres especiales de Base64Url (- y _) por Base64 estándar (+ y /)
    const base64 = str.replace(/-/g, "+").replace(/_/g, "/");
    // 2. Completa con '=' al final si hace falta relleno (padding)
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    // 3. Decodifica de Base64 a binario y luego a texto UTF-8 (soporta acentos y ñ)
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
    return new TextDecoder().decode(bytes);
}

/**
 * CÓMO SE SEPARA EL TOKEN JWT:
 * Un JWT tiene 3 partes separadas por puntos (.): HEADER.PAYLOAD.SIGNATURE
 * Esta función lo divide por el punto "." y decodifica la parte del medio (parts[1] = PAYLOAD).
 */
export function parseJwt(token) {
    try {
        const parts = token.split("."); // Separa el token en sus 3 partes
        if (parts.length !== 3) return null; // Si no tiene 3 partes, no es un JWT válido
        return JSON.parse(decodeBase64Url(parts[1])); // Decodifica el Payload (índice 1) a objeto JSON
    } catch {
        return null;
    }
}

/**
 * Guarda la sesión cuando el usuario inicia sesión exitosamente en login.js.
 * Recibe la respuesta del Backend (POST /api/auth/login -> AuthResponseDto).
 */
export function saveSession(authData) {
    const token = authData?.accessToken ?? null;
    if (!token) {
        throw new Error("La respuesta de autenticación no contiene un token válido.");
    }

    const payload = parseJwt(token) || {};
    const rawRole = authData?.rol ?? payload[ROLE_CLAIM] ?? "";
    const role = Array.isArray(rawRole) ? rawRole[0] : String(rawRole);

    const user = {
        email: payload[EMAIL_CLAIM] ?? "",
        name: authData?.nombre ?? payload[NAME_CLAIM] ?? "",
        role,
        customerId: payload.CustomerId ?? null,
        employeeId: payload.EmployeeId ?? null,
        exp: payload.exp ?? null
    };

    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    return user;
}

/**
 * Devuelve el token JWT guardado en localStorage (usado por apiClient.js para enviarlo a la API).
 */
export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

/**
 * Verifica si el token JWT ya caducó comparando el claim 'exp' (en segundos)
 * contra la fecha/hora actual del navegador (Date.now() en milisegundos).
 */
export function isTokenExpired(token = getToken()) {
    if (!token) return true;
    const payload = parseJwt(token);
    if (!payload) return true;
    if (!payload.exp) return false;
    return Date.now() >= payload.exp * 1000;
}

/**
 * Obtiene los datos del usuario autenticado actual.
 * Si no hay token o ya expiró, limpia la sesión y devuelve null.
 */
export function getUser() {
    const token = getToken();
    if (!token || isTokenExpired(token)) {
        clearSession();
        return null;
    }

    try {
        const raw = localStorage.getItem(USER_KEY);
        if (raw) return JSON.parse(raw);
    } catch {
        // Si falla la lectura de USER_KEY, reconstruye los datos desde el payload del JWT
    }

    const payload = parseJwt(token);
    if (!payload) return null;

    const rawRole = payload[ROLE_CLAIM] ?? "";
    return {
        email: payload[EMAIL_CLAIM] ?? "",
        name: payload[NAME_CLAIM] ?? "",
        role: Array.isArray(rawRole) ? rawRole[0] : String(rawRole),
        customerId: payload.CustomerId ?? null,
        employeeId: payload.EmployeeId ?? null,
        exp: payload.exp ?? null
    };
}

/**
 * Elimina el token y los datos del usuario al cerrar sesión o cuando el token expira.
 */
export function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}
