import { apiClient } from "../services/apiClient.js";
import { redirectIfAuthenticated } from "../guards/authGuard.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

redirectIfAuthenticated("index.html");

const form = document.getElementById("register-form");
const nombreInput = document.getElementById("nombre");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const confirmInput = document.getElementById("confirmPassword");
const alertContainer = document.getElementById("register-alert");
const submitBtn = document.getElementById("btn-submit");

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

    nombreInput.value = nombreInput.value.trim();
    emailInput.value = emailInput.value.trim();
    validatePasswordMatch();

    form.classList.add("was-validated");
    if (!form.checkValidity()) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Registrando...";

    try {
        await apiClient.post(
            "/auth/register",
            {
                nombre: nombreInput.value,
                email: emailInput.value,
                password: passwordInput.value
            },
            { auth: false, skipAuthRedirect: true }
        );

        window.location.replace("login.html?registered=1");
    } catch (error) {
        const variant = error.status === 409 ? "warning" : "danger";
        showInlineAlert(alertContainer, error.message, variant, error.errors);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Crear cuenta";
    }
});