
// ============================================================================
// ARCHIVO: wwwroot/js/utils/ui.js
// PROPÓSITO: Utilidades visuales reutilizables (Toasts, Alertas en línea y Modales
//            de confirmación) basadas en Bootstrap 5 para cumplir con el requisito
//            de "Mensajes adecuados" y "Sanitización / Prevención de XSS".
//            SEGURIDAD ANTI-XSS:
//            Todos los textos dinámicos (mensajes de la API, nombres de clientes o
//            productos y listas de errores) se insertan exclusivamente mediante
//            .textContent sobre nodos creados con document.createElement().
// ============================================================================

// Garantiza que exista el contenedor flotante en la esquina superior derecha para los Toasts
function ensureToastContainer() {
    let container = document.getElementById("app-toast-container");
    if (!container) {
        container = document.createElement("div");
        container.id = "app-toast-container";
        container.className = "toast-container position-fixed top-0 end-0 p-3";
        container.style.zIndex = "1100";
        document.body.appendChild(container);
    }
    return container;
}

/**
 * Muestra una notificación flotante (Toast) en la esquina superior derecha.
 * PREVENCIÓN XSS: Se asigna el mensaje con body.textContent (nunca innerHTML).
 */
export function showToast(message, type = "info") {
    const typeMap = {
        success: "text-bg-success",
        danger: "text-bg-danger",
        warning: "text-bg-warning",
        info: "text-bg-primary"
    };

    const container = ensureToastContainer();

    const toastEl = document.createElement("div");
    toastEl.className = `toast align-items-center border-0 ${typeMap[type] ?? "text-bg-primary"}`;
    toastEl.setAttribute("role", "alert");
    toastEl.setAttribute("aria-live", "assertive");
    toastEl.setAttribute("aria-atomic", "true");

    const wrapper = document.createElement("div");
    wrapper.className = "d-flex";

    const body = document.createElement("div");
    body.className = "toast-body";
    body.textContent = message; // Escapado seguro anti-XSS

    const closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.className = "btn-close btn-close-white me-2 m-auto";
    closeBtn.setAttribute("data-bs-dismiss", "toast");
    closeBtn.setAttribute("aria-label", "Cerrar");

    wrapper.append(body, closeBtn);
    toastEl.appendChild(wrapper);
    container.appendChild(toastEl);

    const toast = new window.bootstrap.Toast(toastEl, { delay: 4500 });
    toastEl.addEventListener("hidden.bs.toast", () => toastEl.remove());
    toast.show();
}

/**
 * Muestra una alerta de Bootstrap dentro de un contenedor de la página o formulario
 * (por ejemplo, errores de credenciales, cuenta bloqueada 423 o registro duplicado 409).
 */
export function showInlineAlert(container, message, type = "danger", details = []) {
    if (!container) return;
    container.replaceChildren();

    const alertDiv = document.createElement("div");
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.setAttribute("role", "alert");

    const mainText = document.createElement("div");
    mainText.textContent = message; // Escapado seguro anti-XSS
    alertDiv.appendChild(mainText);

    if (Array.isArray(details) && details.length > 0) {
        const list = document.createElement("ul");
        list.className = "mb-0 mt-2 ps-3";
        details.forEach((item) => {
            const li = document.createElement("li");
            li.textContent = item; // Escapado seguro anti-XSS
            list.appendChild(li);
        });
        alertDiv.appendChild(list);
    }

    const closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.className = "btn-close";
    closeBtn.setAttribute("data-bs-dismiss", "alert");
    closeBtn.setAttribute("aria-label", "Cerrar");
    alertDiv.appendChild(closeBtn);

    container.appendChild(alertDiv);
}

export function clearInlineAlert(container) {
    if (container) container.replaceChildren();
}

/**
 * Muestra un modal de confirmación de Bootstrap (usado antes de dar de baja lógica
 * un cliente o vaciar el carrito) y devuelve una Promesa booleana (true / false).
 */
export function showConfirmModal({
    title = "Confirmar acción",
    message = "¿Deseas continuar?",
    confirmText = "Confirmar",
    confirmVariant = "danger"
}) {
    return new Promise((resolve) => {
        const modalEl = document.createElement("div");
        modalEl.className = "modal fade";
        modalEl.tabIndex = -1;
        modalEl.setAttribute("aria-hidden", "true");

        const dialog = document.createElement("div");
        dialog.className = "modal-dialog modal-dialog-centered";

        const content = document.createElement("div");
        content.className = "modal-content";

        const header = document.createElement("div");
        header.className = "modal-header";

        const titleEl = document.createElement("h5");
        titleEl.className = "modal-title";
        titleEl.textContent = title;

        const closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "btn-close";
        closeBtn.setAttribute("data-bs-dismiss", "modal");
        closeBtn.setAttribute("aria-label", "Cerrar");

        header.append(titleEl, closeBtn);

        const body = document.createElement("div");
        body.className = "modal-body";
        const bodyP = document.createElement("p");
        bodyP.className = "mb-0";
        bodyP.textContent = message;
        body.appendChild(bodyP);

        const footer = document.createElement("div");
        footer.className = "modal-footer";

        const cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "btn btn-secondary";
        cancelBtn.setAttribute("data-bs-dismiss", "modal");
        cancelBtn.textContent = "Cancelar";

        const okBtn = document.createElement("button");
        okBtn.type = "button";
        okBtn.className = `btn btn-${confirmVariant}`;
        okBtn.textContent = confirmText;

        footer.append(cancelBtn, okBtn);
        content.append(header, body, footer);
        dialog.appendChild(content);
        modalEl.appendChild(dialog);
        document.body.appendChild(modalEl);

        let decided = false;
        const modal = new window.bootstrap.Modal(modalEl);

        okBtn.addEventListener("click", () => {
            decided = true;
            modal.hide();
            resolve(true);
        });

        modalEl.addEventListener("hidden.bs.modal", () => {
            modalEl.remove();
            if (!decided) resolve(false);
        });

        modal.show();
    });
}
