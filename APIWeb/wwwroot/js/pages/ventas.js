// ============================================================================
// ARCHIVO: wwwroot/js/pages/ventas.js
// PROPÓSITO: Historial de Ventas / Mis Pedidos (GET /api/Orders y GET /api/Orders/{id}).
//            AQUÍ SE HACE:
//            1. Revisión de acceso por URL (requireRoles(["Customer", "Employee", "Admin"])).
//            2. Petición GET /api/Orders al Backend (OrderService.ObtenerOrdenAsync):
//               - Si el usuario es Customer, el Backend filtra automáticamente por el
//                 CustomerId firmado en su JWT para que solo vea sus propios pedidos.
//               - Si es Employee o Admin, recibe el historial general de ventas.
//            3. Muestra QUIÉN VENDIÓ (formatSeller):
//               - Toma order.employeeName que viene del JOIN entre 'orders' y 'employees'
//                 en OrderService.cs.
//               - Si no hay EmployeeId (porque la compra la hizo el cliente por su cuenta),
//                 muestra "Venta en línea".
//            4. Muestra EL DESTINO (formatDestination):
//               - Toma order.shipCity, order.shipCountry y order.shipAddress que el Backend
//                 obtuvo directamente de la tabla 'customers' en MySQL.
//            5. Al pulsar "Ver detalle", llama a GET /api/Orders/{orderId} y llena el modal
//               usando document.createElement y .textContent para prevenir XSS.
// ============================================================================

import { renderNavbar } from "../components/navbar.js?v=2";
import { requireRoles } from "../guards/authGuard.js";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showInlineAlert } from "../utils/ui.js";

// REVISIÓN DE ACCESO POR URL: Verifica sesión activa y rol permitido
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

// Si quien entra es un Cliente, cambia el título a "Mis Pedidos"
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

/**
 * AQUÍ SE FORMATEA QUIÉN VENDIÓ LA ORDEN:
 * - DE DÓNDE SE TOMA: De order.employeeName (FirstName + LastName de la tabla 'employees'
 *   en MySQL, unido en OrderService.cs mediante .Include(o => o.Employee)).
 * - POR QUÉ PUEDE DECIR "Venta en línea": Cuando un cliente (rol Customer) compra
 *   directamente desde su cuenta sin intervención de un empleado, EmployeeId es null.
 */
function formatSeller(order) {
    if (order.employeeName) return order.employeeName;
    if (order.employeeId) return `Empleado #${order.employeeId}`;
    return "Venta en línea";
}

/**
 * AQUÍ SE FORMATEA EL DESTINO DE LA ORDEN:
 * - DE DÓNDE SE TOMA: De order.shipAddress, order.shipCity y order.shipCountry,
 *   los cuales OrderService.cs toma directamente del registro del cliente en la
 *   tabla 'customers' de MySQL (sin pedirlos en campos de texto en el POS).
 */
function formatDestination(order, includeAddress = false) {
    const parts = includeAddress
        ? [order.shipAddress, order.shipCity, order.shipCountry]
        : [order.shipCity, order.shipCountry];
    const joined = parts.filter(Boolean).join(", ");
    if (joined) return joined;
    return order.shipAddress || "Sin especificar";
}

/**
 * Renderiza la tabla principal de órdenes.
 * PREVENCIÓN XSS: Se construyen nodos DOM con createElement y se asigna texto con textContent.
 */
function renderOrders(list) {
    ordersTbody.replaceChildren();
    ordersCount.textContent = `${list.length} ${list.length === 1 ? "orden" : "órdenes"}`;

    if (list.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 7;
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

        // Nombre del cliente tomado de Customer.CompanyName en la BD
        const tdCustomer = document.createElement("td");
        tdCustomer.textContent = order.companyName || order.customerId || "-";

        // Nombre del empleado que realizó la venta tomado de Employee en la BD
        const tdSeller = document.createElement("td");
        tdSeller.textContent = formatSeller(order);

        const tdDate = document.createElement("td");
        tdDate.textContent = formatDate(order.orderDate);

        // Destino tomado de los datos del cliente en la BD
        const tdDest = document.createElement("td");
        tdDest.textContent = formatDestination(order);

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
        tr.append(tdId, tdCustomer, tdSeller, tdDate, tdDest, tdTotal, tdActions);
        ordersTbody.appendChild(tr);
    });
}

function renderOrdersMessage(message) {
    ordersTbody.replaceChildren();
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 7;
    td.className = "text-center text-muted py-4";
    td.textContent = message;
    tr.appendChild(td);
    ordersTbody.appendChild(tr);
}

/**
 * PETICIÓN A LA API: GET /api/Orders -> OrdersController.GetOrders -> OrderService.ObtenerOrdenAsync
 */
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

/**
 * PETICIÓN A LA API: GET /api/Orders/{orderId} -> OrdersController.GetById -> OrderService.ObtenerPorIdAsync
 * Abre el modal y muestra Cliente, Vendedor (empleado), Fecha, Destino (de la BD) y los productos vendidos.
 */
async function openOrderDetail(orderId) {
    clearInlineAlert(modalAlert);
    detailItemsTbody.replaceChildren();
    document.getElementById("orderDetailModalLabel").textContent = `Detalle de Orden #${orderId}`;
    document.getElementById("detail-customer").textContent = "Cargando...";
    document.getElementById("detail-employee").textContent = "-";
    document.getElementById("detail-date").textContent = "-";
    document.getElementById("detail-destination").textContent = "-";
    document.getElementById("detail-total").textContent = "0.00";
    orderDetailModal.show();

    try {
        const order = await apiClient.get(`/Orders/${orderId}`, { skipAuthRedirect: true });

        document.getElementById("detail-customer").textContent = order.companyName || order.customerId || "-";
        document.getElementById("detail-employee").textContent = formatSeller(order);
        document.getElementById("detail-date").textContent = formatDate(order.orderDate);
        document.getElementById("detail-destination").textContent = formatDestination(order, true);
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

// Buscador en tiempo real por ID de orden, cliente, empleado vendedor o destino
searchInput.addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    const filtered = ordersCache.filter((o) =>
        String(o.orderId).includes(q) ||
        (o.companyName || "").toLowerCase().includes(q) ||
        (o.customerId || "").toLowerCase().includes(q) ||
        formatSeller(o).toLowerCase().includes(q) ||
        (o.shipAddress || "").toLowerCase().includes(q) ||
        (o.shipCity || "").toLowerCase().includes(q) ||
        (o.shipCountry || "").toLowerCase().includes(q)
    );
    renderOrders(filtered);
});

loadOrders();
