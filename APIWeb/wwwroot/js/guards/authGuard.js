// ============================================================================
// ARCHIVO: authGuard.js
// PROPÓSITO: Revisión de acceso y permisos por URL en el Frontend.
//            Impide que un usuario sin sesión o con un rol no autorizado
//            entre escribiendo directamente la dirección HTML en el navegador.
// ============================================================================

import { clearSession, getToken, getUser, isTokenExpired } from "../services/session.js";

/**
 * Verifica que el usuario tenga una sesión activa y un token JWT vigente.
 * Si no está autenticado, limpia localStorage y lo envía a login.html.
 */
export function requireAuth() {
    const token = getToken();
    const user = getUser();

    if (!token || !user || isTokenExpired(token)) {
        clearSession();
        window.location.replace("login.html");
        return null;
    }

    return user;
}

/**
 * REVISIÓN DE ACCESO POR URL SEGÚN ROL:
 * Recibe un arreglo con los roles permitidos para la página actual (ej. ["Admin", "Employee"]).
 * Si el rol del usuario en el JWT no está dentro de allowedRoles, redirige a acceso-denegado.html.
 */
export function requireRoles(allowedRoles = []) {
    const user = requireAuth();
    if (!user) return null;

    const normalizedAllowed = allowedRoles.map((r) => r.toLowerCase());
    const userRole = (user.role || "").toLowerCase();

    if (normalizedAllowed.length > 0 && !normalizedAllowed.includes(userRole)) {
        window.location.replace("acceso-denegado.html");
        return null;
    }

    return user;
}

/**
 * Se usa en login.html y register.html: si el usuario ya tiene sesión iniciada,
 * lo redirige automáticamente al panel principal (index.html).
 */
export function redirectIfAuthenticated(destination = "index.html") {
    const token = getToken();
    const user = getUser();
    if (token && user && !isTokenExpired(token)) {
        window.location.replace(destination);
        return true;
    }
    return false;
}
