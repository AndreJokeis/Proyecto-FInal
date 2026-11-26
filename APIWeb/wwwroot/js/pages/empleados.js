import { requireRoles } from "../guards/authGuard.js";
import { renderNavbar } from "../components/navbar.js";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showInlineAlert, showToast } from "../utils/ui.js";

const user = requireRoles(["Admin"]);

if (user) {
    renderNavbar();

    const form = document.getElementById("employee-form");
    const alertContainer = document.getElementById("employee-alert");
    const submitBtn = document.getElementById("btn-save-employee");

    const fields = {
        firstName: document.getElementById("firstName"),
        lastName: document.getElementById("lastName"),
        title: document.getElementById("title"),
        role: document.getElementById("role"),
        email: document.getElementById("email"),
        password: document.getElementById("password")
    };

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        clearInlineAlert(alertContainer);

        fields.firstName.value = fields.firstName.value.trim();
        fields.lastName.value = fields.lastName.value.trim();
        fields.title.value = fields.title.value.trim();
        fields.email.value = fields.email.value.trim();

        form.classList.add("was-validated");
        if (!form.checkValidity()) return;

        submitBtn.disabled = true;
        submitBtn.textContent = "Registrando...";

        try {
            // Verificar nombres exactos de propiedades contra el DTO que suba Joseph
            await apiClient.post("/admin/employees", {
                firstName: fields.firstName.value,
                lastName: fields.lastName.value,
                title: fields.title.value || null,
                role: fields.role.value,
                email: fields.email.value,
                password: fields.password.value
            });

            showToast("Empleado registrado correctamente.", "success");
            form.reset();
            form.classList.remove("was-validated");
        } catch (error) {
            if (error.status === 404) {
                showInlineAlert(
                    alertContainer,
                    "El endpoint /api/admin/employees aún no está disponible en el backend.",
                    "info"
                );
            } else {
                const variant = error.status === 409 ? "warning" : "danger";
                showInlineAlert(alertContainer, error.message, variant, error.errors);
            }
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = "Registrar empleado";
        }
    });
}