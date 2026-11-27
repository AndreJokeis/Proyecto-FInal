import { renderNavbar } from "../components/navbar.js";
import { requireRoles } from "../guards/authGuard.js";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

const user = requireRoles(["Customer", "Employee", "Admin"]);
if (!user) {
    throw new Error("Acceso no autorizado.");
}

renderNavbar();

const pageTitle = document.getElementById("page-title");
const pageAlert = document.getElementById("page-alert");
const modalAlert = document.getElementById("modal-alert");
const searchInput = document.getElementById("search-input");
const ordersCount = document.getElementById("orders-count");
const ordersTbody = document.getElementById("orders-tbody");
const detailItemsTbody = document.getElementById("detail-items-tbody");
const orderDetailModal = new window.bootstrap.Modal(document.getElementById("orderDetailModal"));

if ((user.role || "").toLowerCase() === "customer") {
    pageTitle.textContent = "Mis Pedidos";
}

let ordersCache = [];

function formatDate(value) {
    if (!value) return "-";
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? "-" : d.toLocaleDateString("es-MX");
}

function formatMoney(value) {
    return Number(value || 0).toFixed(2);
}

function renderOrders(list) {
    ordersTbody.replaceChildren();
    ordersCount.textContent = `${list.length} ${list.length === 1 ? "orden" : "órdenes"}`;

    if (list.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 6;
        td.className = "text-center text-muted py-4";
        td.textContent = "No hay órdenes registradas.";
        tr.appendChild(td);
        ordersTbody.appendChild(tr);
        return;
    }

    list.forEach((order) => {
        const tr = document.createElement("tr");

        const tdId = document.createElement("td");
        tdId.className = "fw-semibold";
        tdId.textContent = `#${order.orderId}`;

        const tdCustomer = document.createElement("td");
        tdCustomer.textContent = order.companyName || order.customerId || "-";

        const tdDate = document.createElement("td");
        tdDate.textContent = formatDate(order.orderDate);

        const tdDest = document.createElement("td");
        tdDest.textContent = [order.shipCity, order.shipCountry].filter(Boolean).join(", ") || "-";

        const tdTotal = document.createElement("td");
        tdTotal.className = "text-end fw-semibold";
        tdTotal.textContent = `$${formatMoney(order.total)}`;

        const tdActions = document.createElement("td");
        tdActions.className = "text-end";

        const btnDetail = document.createElement("button");
        btnDetail.type = "button";
        btnDetail.className = "btn btn-sm btn-outline-primary";
        btnDetail.textContent = "Ver detalle";
        btnDetail.addEventListener("click", () => openOrderDetail(order.orderId));

        tdActions.appendChild(btnDetail);
        tr.append(tdId, tdCustomer, tdDate, tdDest, tdTotal, tdActions);
        ordersTbody.appendChild(tr);
    });
}

function renderOrdersMessage(message) {
    ordersTbody.replaceChildren();
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 6;
    td.className = "text-center text-muted py-4";
    td.textContent = message;
    tr.appendChild(td);
    ordersTbody.appendChild(tr);
}

async function loadOrders() {
    clearInlineAlert(pageAlert);
    renderOrdersMessage("Cargando órdenes...");

    try {
        const data = await apiClient.get("/Orders");
        ordersCache = Array.isArray(data) ? data : [];
        renderOrders(ordersCache);
    } catch (error) {
        renderOrdersMessage("No fue posible cargar las órdenes.");
        showInlineAlert(pageAlert, error.message || "Error al cargar las órdenes.", "danger", error.errors);
    }
}

async function openOrderDetail(orderId) {
    clearInlineAlert(modalAlert);
    detailItemsTbody.replaceChildren();
    document.getElementById("orderDetailModalLabel").textContent = `Detalle de Orden #${orderId}`;
    document.getElementById("detail-customer").textContent = "Cargando...";
    document.getElementById("detail-date").textContent = "-";
    document.getElementById("detail-destination").textContent = "-";
    document.getElementById("detail-employee").textContent = "-";
    document.getElementById("detail-total").textContent = "0.00";
    orderDetailModal.show();

    try {
        const order = await apiClient.get(`/Orders/${orderId}`, { skipAuthRedirect: true });

        document.getElementById("detail-customer").textContent = order.companyName || order.customerId || "-";
        document.getElementById("detail-date").textContent = formatDate(order.orderDate);
        document.getElementById("detail-destination").textContent = [order.shipCity, order.shipCountry].filter(Boolean).join(", ") || "-";
        document.getElementById("detail-employee").textContent = order.employeeId ?? "-";
        document.getElementById("detail-total").textContent = formatMoney(order.total);

        const items = Array.isArray(order.items) ? order.items : [];
        if (items.length === 0) {
            const tr = document.createElement("tr");
            const td = document.createElement("td");
            td.colSpan = 4;
            td.className = "text-center text-muted";
            td.textContent = "Esta orden no tiene productos registrados.";
            tr.appendChild(td);
            detailItemsTbody.appendChild(tr);
            return;
        }

        items.forEach((item) => {
            const tr = document.createElement("tr");

            const tdName = document.createElement("td");
            tdName.textContent = item.productName;

            const tdPrice = document.createElement("td");
            tdPrice.className = "text-end";
            tdPrice.textContent = `$${formatMoney(item.unitPrice)}`;

            const tdQty = document.createElement("td");
            tdQty.className = "text-center";
            tdQty.textContent = item.quantity;

            const tdSub = document.createElement("td");
            tdSub.className = "text-end";
            tdSub.textContent = `$${formatMoney(item.subtotal)}`;

            tr.append(tdName, tdPrice, tdQty, tdSub);
            detailItemsTbody.appendChild(tr);
        });
    } catch (error) {
        showInlineAlert(modalAlert, error.message || "No se pudo cargar el detalle de la orden.", "danger", error.errors);
    }
}

searchInput.addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    const filtered = ordersCache.filter((o) =>
        String(o.orderId).includes(q) ||
        (o.companyName || "").toLowerCase().includes(q) ||
        (o.customerId || "").toLowerCase().includes(q) ||
        (o.shipCity || "").toLowerCase().includes(q) ||
        (o.shipCountry || "").toLowerCase().includes(q)
    );
    renderOrders(filtered);
});

loadOrders();