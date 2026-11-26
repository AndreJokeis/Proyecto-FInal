import { clearSession, getToken, getUser, isTokenExpired } from "../services/session.js";

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

export function redirectIfAuthenticated(destination = "index.html") {
    const token = getToken();
    const user = getUser();
    if (token && user && !isTokenExpired(token)) {
        window.location.replace(destination);
        return true;
    }
    return false;
}