// ============================================================================
// ARCHIVO: empleados.js
// PROPÓSITO: Alta exclusiva de Empleados y Administradores por un Admin.
//            Envía POST /api/admin/employees -> crea registro en 'employees'
//            y cuenta en 'usuarios' vinculada por EmployeeId.
// ============================================================================

import { requireRoles } from "../guards/authGuard.js";
import { renderNavbar } from "../components/navbar.js?v=2";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showInlineAlert, showToast } from "../utils/ui.js";

// REVISIÓN DE ACCESO POR URL: Solo el rol "Admin" puede permanecer en empleados.html.
// Si un "Employee" o "Customer" escribe esta URL, authGuard.js lo redirige a acceso-denegado.html.
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

        // Sanitización de entradas con .trim()
        fields.firstName.value = fields.firstName.value.trim();
        fields.lastName.value = fields.lastName.value.trim();
        fields.title.value = fields.title.value.trim();
        fields.email.value = fields.email.value.trim();

        form.classList.add("was-validated");
        if (!form.checkValidity()) return;

        submitBtn.disabled = true;
        submitBtn.textContent = "Registrando...";

        try {
            // PETICIÓN A LA API: POST /api/admin/employees -> AdminEmployeesController.cs -> EmployeeService.cs
            // apiClient adjunta automáticamente el JWT de Admin en Authorization: Bearer <token>
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
            // Si el correo ya existe, el Backend devuelve 409 Conflict
            const variant = error.status === 409 ? "warning" : "danger";
            showInlineAlert(alertContainer, error.message, variant, error.errors);
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = "Registrar empleado";
        }
    });
}
