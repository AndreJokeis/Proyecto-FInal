import { requireRoles } from "../guards/authGuard.js";
import { renderNavbar } from "../components/navbar.js";
import { apiClient } from "../services/apiClient.js";
import {
    clearInlineAlert,
    showConfirmModal,
    showInlineAlert,
    showToast
} from "../utils/ui.js";

const user = requireRoles(["Admin", "Employee"]);

if (user) {
    renderNavbar();

    const tbody = document.getElementById("customers-tbody");
    const countBadge = document.getElementById("customers-count");
    const searchInput = document.getElementById("search-input");
    const pageAlert = document.getElementById("page-alert");
    const modalAlert = document.getElementById("modal-alert");

    const modalEl = document.getElementById("customerModal");
    const modalTitle = document.getElementById("customerModalLabel");
    const customerModal = new window.bootstrap.Modal(modalEl);

    const form = document.getElementById("customer-form");
    const btnNew = document.getElementById("btn-new-customer");
    const btnSave = document.getElementById("btn-save-customer");

    const fields = {
        customerId: document.getElementById("customerId"),
        companyName: document.getElementById("companyName"),
        contactName: document.getElementById("contactName"),
        contactTitle: document.getElementById("contactTitle"),
        address: document.getElementById("address"),
        city: document.getElementById("city"),
        region: document.getElementById("region"),
        postalCode: document.getElementById("postalCode"),
        country: document.getElementById("country"),
        phone: document.getElementById("phone")
    };

    let allCustomers = [];
    let editingCustomerId = null;

    function toNullIfEmpty(value) {
        const trimmed = (value || "").trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    function renderEmptyRow(message) {
        tbody.replaceChildren();
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 6;
        td.className = "text-center text-muted py-4";
        td.textContent = message;
        tr.appendChild(td);
        tbody.appendChild(tr);
    }

    function renderTable(list) {
        countBadge.textContent = `${list.length} cliente${list.length === 1 ? "" : "s"}`;

        if (list.length === 0) {
            renderEmptyRow("No se encontraron clientes activos.");
            return;
        }

        tbody.replaceChildren();

        list.forEach((c) => {
            const tr = document.createElement("tr");

            const tdId = document.createElement("td");
            const code = document.createElement("span");
            code.className = "badge text-bg-light border text-dark";
            code.textContent = c.customerId || "";
            tdId.appendChild(code);

            const tdCompany = document.createElement("td");
            tdCompany.className = "fw-semibold";
            tdCompany.textContent = c.companyName || "";

            const tdContact = document.createElement("td");
            const contactMain = document.createElement("div");
            contactMain.textContent = c.contactName || "—";
            tdContact.appendChild(contactMain);
            if (c.contactTitle) {
                const contactSub = document.createElement("small");
                contactSub.className = "text-muted";
                contactSub.textContent = c.contactTitle;
                tdContact.appendChild(contactSub);
            }

            const tdLocation = document.createElement("td");
            const locationParts = [c.city, c.country].filter(Boolean);
            tdLocation.textContent = locationParts.length > 0 ? locationParts.join(", ") : "—";

            const tdPhone = document.createElement("td");
            tdPhone.textContent = c.phone || "—";

            const tdActions = document.createElement("td");
            tdActions.className = "text-end text-nowrap";

            const editBtn = document.createElement("button");
            editBtn.type = "button";
            editBtn.className = "btn btn-outline-primary btn-sm me-1";
            editBtn.textContent = "Editar";
            editBtn.addEventListener("click", () => openEditModal(c));

            const deleteBtn = document.createElement("button");
            deleteBtn.type = "button";
            deleteBtn.className = "btn btn-outline-danger btn-sm";
            deleteBtn.textContent = "Eliminar";
            deleteBtn.addEventListener("click", () => handleSoftDelete(c));

            tdActions.append(editBtn, deleteBtn);
            tr.append(tdId, tdCompany, tdContact, tdLocation, tdPhone, tdActions);
            tbody.appendChild(tr);
        });
    }

    function applyFilter() {
        const q = searchInput.value.trim().toLowerCase();
        if (!q) {
            renderTable(allCustomers);
            return;
        }

        const filtered = allCustomers.filter((c) => {
            const haystack = [
                c.customerId,
                c.companyName,
                c.contactName,
                c.city,
                c.country,
                c.phone
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();
            return haystack.includes(q);
        });

        renderTable(filtered);
    }

    async function loadCustomers() {
        clearInlineAlert(pageAlert);
        renderEmptyRow("Cargando clientes...");

        try {
            const data = await apiClient.get("/customers");
            allCustomers = Array.isArray(data) ? data : [];
            applyFilter();
        } catch (error) {
            renderEmptyRow("No fue posible cargar los clientes.");
            showInlineAlert(pageAlert, error.message, "danger", error.errors);
        }
    }

    function resetForm() {
        form.reset();
        form.classList.remove("was-validated");
        clearInlineAlert(modalAlert);
    }

    function openCreateModal() {
        editingCustomerId = null;
        resetForm();
        modalTitle.textContent = "Nuevo cliente";
        fields.customerId.disabled = false;
        customerModal.show();
    }

    function openEditModal(customer) {
        editingCustomerId = customer.customerId;
        resetForm();
        modalTitle.textContent = `Editar cliente (${customer.customerId})`;

        fields.customerId.value = customer.customerId || "";
        fields.customerId.disabled = true;
        fields.companyName.value = customer.companyName || "";
        fields.contactName.value = customer.contactName || "";
        fields.contactTitle.value = customer.contactTitle || "";
        fields.address.value = customer.address || "";
        fields.city.value = customer.city || "";
        fields.region.value = customer.region || "";
        fields.postalCode.value = customer.postalCode || "";
        fields.country.value = customer.country || "";
        fields.phone.value = customer.phone || "";

        customerModal.show();
    }

    async function handleSoftDelete(customer) {
        const confirmed = await showConfirmModal({
            title: "Confirmar baja lógica",
            message: `¿Deseas dar de baja al cliente "${customer.companyName}" (${customer.customerId})? Desaparecerá de los listados activos, pero su historial de pedidos se conservará intacto.`,
            confirmText: "Dar de baja",
            confirmVariant: "danger"
        });

        if (!confirmed) return;

        try {
            const res = await apiClient.delete(`/customers/${encodeURIComponent(customer.customerId)}`);
            showToast(res?.message || "Cliente dado de baja correctamente.", "success");
            await loadCustomers();
        } catch (error) {
            showToast(error.message, "danger");
        }
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        clearInlineAlert(modalAlert);

        fields.customerId.value = fields.customerId.value.trim().toUpperCase();
        fields.companyName.value = fields.companyName.value.trim();

        form.classList.add("was-validated");
        if (!form.checkValidity()) return;

        const basePayload = {
            companyName: fields.companyName.value,
            contactName: toNullIfEmpty(fields.contactName.value),
            contactTitle: toNullIfEmpty(fields.contactTitle.value),
            address: toNullIfEmpty(fields.address.value),
            city: toNullIfEmpty(fields.city.value),
            region: toNullIfEmpty(fields.region.value),
            postalCode: toNullIfEmpty(fields.postalCode.value),
            country: toNullIfEmpty(fields.country.value),
            phone: toNullIfEmpty(fields.phone.value)
        };

        btnSave.disabled = true;
        btnSave.textContent = "Guardando...";

        try {
            if (editingCustomerId) {
                await apiClient.put(
                    `/customers/${encodeURIComponent(editingCustomerId)}`,
                    basePayload
                );
                showToast("Cliente actualizado correctamente.", "success");
            } else {
                await apiClient.post("/customers", {
                    customerId: fields.customerId.value,
                    ...basePayload
                });
                showToast("Cliente registrado correctamente.", "success");
            }

            customerModal.hide();
            await loadCustomers();
        } catch (error) {
            const variant = error.status === 409 ? "warning" : "danger";
            showInlineAlert(modalAlert, error.message, variant, error.errors);
        } finally {
            btnSave.disabled = false;
            btnSave.textContent = "Guardar";
        }
    });

    btnNew.addEventListener("click", openCreateModal);
    searchInput.addEventListener("input", applyFilter);

    loadCustomers();
}