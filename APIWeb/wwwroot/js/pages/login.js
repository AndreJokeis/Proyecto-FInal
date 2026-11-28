// ============================================================================
// ARCHIVO: wwwroot/js/pages/login.js
// PROPÓSITO: Controla la pantalla de Inicio de Sesión (wwwroot/login.html).
//            AQUÍ SE HACE PASO A PASO:
//            1. Si el usuario llega desde "Cambiar cuenta" (?switch=1), borra la sesión previa.
//            2. Si ya tiene un JWT válido y vigente, lo redirige directo a index.html.
//            3. Muestra mensajes adecuados si su sesión caducó (?reason=session) o si
//               acaba de registrarse como cliente (?registered=1).
//            4. Al enviar el formulario, sanitiza el correo con .trim(), valida los campos
//               con Bootstrap 5 y envía POST /api/auth/login (AuthController -> AuthService).
//            5. Si las credenciales son válidas, guarda el JWT en localStorage (saveSession)
//               y redirige al Panel Principal (index.html).
//            6. Si acumula 3 intentos fallidos, muestra la alerta amarilla del bloqueo
//               temporal de 15 minutos devuelto por la API con código HTTP 423 (Locked).
// ============================================================================

import { apiClient } from "../services/apiClient.js";
import { clearSession, saveSession } from "../services/session.js";
import { redirectIfAuthenticated } from "../guards/authGuard.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

// Si viene desde "Cambiar cuenta" (?switch=1), limpia la sesión actual primero
const params = new URLSearchParams(window.location.search);
if (params.get("switch") === "1") {
    clearSession();
}

// Si el usuario ya tiene un token JWT activo, lo manda directo a index.html
redirectIfAuthenticated("index.html");

const form = document.getElementById("login-form");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const alertContainer = document.getElementById("login-alert");
const submitBtn = document.getElementById("btn-submit");

// Muestra avisos si fue redirigido por sesión expirada (401) o tras registrarse
if (params.get("reason") === "session") {
    showInlineAlert(alertContainer, "Tu sesión expiró. Vuelve a iniciar sesión.", "warning");
} else if (params.get("registered") === "1") {
    showInlineAlert(alertContainer, "Cuenta creada correctamente. Ya puedes iniciar sesión.", "success");
}

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    clearInlineAlert(alertContainer);

    // Sanitización básica: elimina espacios sobrantes del correo
    emailInput.value = emailInput.value.trim();
    form.classList.add("was-validated");

    if (!form.checkValidity()) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Verificando...";

    try {
        // PETICIÓN A LA API: POST /api/auth/login -> AuthController.cs -> AuthService.cs
        // auth: false porque aún no tenemos token; skipAuthRedirect: true para manejar el error 401 aquí mismo
        const data = await apiClient.post(
            "/auth/login",
            {
                email: emailInput.value,
                password: passwordInput.value
            },
            { auth: false, skipAuthRedirect: true }
        );

        // Guarda el token JWT devuelto por la API en localStorage (session.js)
        saveSession(data);
        window.location.replace("index.html");
    } catch (error) {
        // BLOQUEO POR 3 INTENTOS FALLIDOS:
        // Si el Backend devuelve HTTP 423 (Locked), muestra alerta amarilla con los 15 minutos de bloqueo
        const variant = error.status === 423 || error.status === 429 ? "warning" : "danger";
        showInlineAlert(alertContainer, error.message, variant, error.errors);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Iniciar sesión";
    }
});
