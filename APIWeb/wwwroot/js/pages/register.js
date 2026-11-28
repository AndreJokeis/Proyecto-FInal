// ============================================================================
// ARCHIVO: wwwroot/js/pages/register.js
// PROPÓSITO: Controla el Registro Autónomo de Clientes (wwwroot/register.html).
//            AQUÍ SE HACE PASO A PASO:
//            1. Verifica con redirectIfAuthenticated si ya hay sesión iniciada.
//            2. Valida en tiempo real que "Contraseña" y "Confirmar contraseña" coincidan.
//            3. Sanitiza nombre y correo con .trim() antes de validar el formulario.
//            4. Envía POST /api/auth/register (AuthController -> AuthService.RegistrarAsync)
//               mandando ÚNICAMENTE { nombre, email, password } (sin rol, evitando
//               escalación de privilegios).
//            5. Si el Backend detecta correo duplicado (HTTP 409 Conflict), muestra una
//               alerta preventiva; si tiene éxito (HTTP 201), redirige a login.html?registered=1.
// ============================================================================

import { apiClient } from "../services/apiClient.js";
import { redirectIfAuthenticated } from "../guards/authGuard.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

// Si ya tiene sesión activa, no necesita registrarse; redirige a index.html
redirectIfAuthenticated("index.html");

const form = document.getElementById("register-form");
const nombreInput = document.getElementById("nombre");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const confirmInput = document.getElementById("confirmPassword");
const alertContainer = document.getElementById("register-alert");
const submitBtn = document.getElementById("btn-submit");

/**
 * Valida en tiempo real que los campos "Contraseña" y "Confirmar contraseña" coincidan.
 */
function validatePasswordMatch() {
    if (confirmInput.value !== passwordInput.value) {
        confirmInput.setCustomValidity("Las contraseñas no coinciden.");
    } else {
        confirmInput.setCustomValidity("");
    }
}

passwordInput.addEventListener("input", validatePasswordMatch);
confirmInput.addEventListener("input", validatePasswordMatch);

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    clearInlineAlert(alertContainer);

    // Sanitización de entradas: elimina espacios al inicio y final
    nombreInput.value = nombreInput.value.trim();
    emailInput.value = emailInput.value.trim();
    validatePasswordMatch();

    form.classList.add("was-validated");
    if (!form.checkValidity()) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Registrando...";

    try {
        // PETICIÓN A LA API: POST /api/auth/register -> AuthController.cs -> AuthService.RegistrarAsync
        // Envía únicamente los datos necesarios (RegisterRequestDto)
        await apiClient.post(
            "/auth/register",
            {
                nombre: nombreInput.value,
                email: emailInput.value,
                password: passwordInput.value
            },
            { auth: false, skipAuthRedirect: true }
        );

        // Redirige al login indicando que la cuenta fue creada con éxito
        window.location.replace("login.html?registered=1");
    } catch (error) {
        // PREVENCIÓN DE DUPLICADOS: Si el correo ya existe, el Backend devuelve HTTP 409 Conflict
        const variant = error.status === 409 ? "warning" : "danger";
        showInlineAlert(alertContainer, error.message, variant, error.errors);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Crear cuenta";
    }
});
