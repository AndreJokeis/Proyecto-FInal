import { apiClient } from "../services/apiClient.js";
import { saveSession } from "../services/session.js";
import { redirectIfAuthenticated } from "../guards/authGuard.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

redirectIfAuthenticated("index.html");

const form = document.getElementById("login-form");
const emailInput = document.getElementById("email");
const passwordInput = document.getElementById("password");
const alertContainer = document.getElementById("login-alert");
const submitBtn = document.getElementById("btn-submit");

const params = new URLSearchParams(window.location.search);
if (params.get("reason") === "session") {
    showInlineAlert(alertContainer, "Tu sesión expiró. Vuelve a iniciar sesión.", "warning");
} else if (params.get("registered") === "1") {
    showInlineAlert(alertContainer, "Cuenta creada correctamente. Ya puedes iniciar sesión.", "success");
}

form.addEventListener("submit", async (e) => {
    e.preventDefault();
    clearInlineAlert(alertContainer);

    emailInput.value = emailInput.value.trim();
    form.classList.add("was-validated");

    if (!form.checkValidity()) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Verificando...";

    try {
        const data = await apiClient.post(
            "/auth/login",
            {
                email: emailInput.value,
                password: passwordInput.value
            },
            { auth: false, skipAuthRedirect: true }
        );

        saveSession(data);
        window.location.replace("index.html");
    } catch (error) {
        const variant = error.status === 423 || error.status === 429 ? "warning" : "danger";
        showInlineAlert(alertContainer, error.message, variant, error.errors);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Iniciar sesión";
    }
});