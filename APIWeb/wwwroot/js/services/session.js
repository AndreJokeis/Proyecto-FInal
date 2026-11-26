const TOKEN_KEY = "pos_auth_token";
const USER_KEY = "pos_auth_user";

const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const NAME_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/name";
const EMAIL_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress";

function decodeBase64Url(str) {
    const base64 = str.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
    return new TextDecoder().decode(bytes);
}

export function parseJwt(token) {
    try {
        const parts = token.split(".");
        if (parts.length !== 3) return null;
        return JSON.parse(decodeBase64Url(parts[1]));
    } catch {
        return null;
    }
}

export function saveSession(authData) {
    const token = authData?.token ?? authData?.accessToken ?? authData?.jwt ?? null;
    if (!token) {
        throw new Error("La respuesta de autenticación no contiene un token válido.");
    }

    const payload = parseJwt(token) || {};
    const rawRole = authData?.role ?? authData?.rol ?? payload[ROLE_CLAIM] ?? payload.role ?? payload.rol ?? "";
    const role = Array.isArray(rawRole) ? rawRole[0] : String(rawRole);

    const user = {
        email: authData?.email ?? payload[EMAIL_CLAIM] ?? payload.email ?? "",
        name: authData?.fullName ?? authData?.nombre ?? payload[NAME_CLAIM] ?? payload.unique_name ?? "",
        role,
        customerId: authData?.customerId ?? payload.CustomerId ?? payload.customerId ?? null,
        employeeId: authData?.employeeId ?? payload.EmployeeId ?? payload.employeeId ?? null,
        exp: payload.exp ?? null
    };

    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    return user;
}

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

export function isTokenExpired(token = getToken()) {
    if (!token) return true;
    const payload = parseJwt(token);
    if (!payload) return true;
    if (!payload.exp) return false;
    return Date.now() >= payload.exp * 1000;
}

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
        // Fallback al payload del JWT
    }

    const payload = parseJwt(token);
    if (!payload) return null;

    const rawRole = payload[ROLE_CLAIM] ?? payload.role ?? payload.rol ?? "";
    return {
        email: payload[EMAIL_CLAIM] ?? payload.email ?? "",
        name: payload[NAME_CLAIM] ?? payload.unique_name ?? "",
        role: Array.isArray(rawRole) ? rawRole[0] : String(rawRole),
        customerId: payload.CustomerId ?? payload.customerId ?? null,
        employeeId: payload.EmployeeId ?? payload.employeeId ?? null,
        exp: payload.exp ?? null
    };
}

export function clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
}