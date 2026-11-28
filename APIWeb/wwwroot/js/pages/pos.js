// ============================================================================
// ARCHIVO: wwwroot/js/pages/pos.js
// PROPÓSITO: Punto de Venta y Carrito de Compras.
//            AQUÍ SE HACE:
//            1. Revisión de acceso por URL (requireRoles(["Customer", "Employee", "Admin"])).
//            2. Si el usuario es Employee o Admin (isStaff), muestra el selector de
//               clientes (GET /api/Customers). Si es Customer, oculta ese selector
//               porque el Backend toma su CustomerId directamente del JWT.
//            3. Carga el catálogo activo (GET /api/Products) y deshabilita el botón
//               "+ Agregar" en productos descontinuados o con stock <= 0.
//            4. Controla el carrito en memoria validando que la cantidad nunca
//               supere el stock disponible (maxStock).
//            5. Al confirmar la venta (POST /api/Orders):
//               - Envía ÚNICAMENTE { customerId, items: [{ productId, quantity }] }.
//               - NO envía precios ni dirección de destino: el Backend toma el
//                 UnitPrice desde la tabla 'products', el vendedor desde el
//                 EmployeeId del JWT y el destino desde la tabla 'customers' en MySQL.
// ============================================================================

import { renderNavbar } from "../components/navbar.js?v=2";
import { requireRoles } from "../guards/authGuard.js";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showConfirmModal, showInlineAlert, showToast } from "../utils/ui.js";

// REVISIÓN DE ACCESO POR URL
const user = requireRoles(["Customer", "Employee", "Admin"]);
if (!user) {
    throw new Error("Acceso no autorizado.");
}

renderNavbar();

const roleLower = (user.role || "").toLowerCase();
const isStaff = roleLower === "admin" || roleLower === "employee";

const pageTitle = document.getElementById("page-title");
const pageAlert = document.getElementById("page-alert");
const searchInput = document.getElementById("search-input");
const productsTbody = document.getElementById("products-tbody");
const customerSection = document.getElementById("customer-section");
const customerSelect = document.getElementById("customer-select");
const cartTbody = document.getElementById("cart-tbody");
const cartTotal = document.getElementById("cart-total");
const btnClearCart = document.getElementById("btn-clear-cart");
const btnConfirmSale = document.getElementById("btn-confirm-sale");

// Si es empleado o administrador, habilita el selector para elegir a qué cliente se le vende
if (isStaff) {
    customerSection.classList.remove("d-none");
} else {
    pageTitle.textContent = "Realizar Compra";
    btnConfirmSale.textContent = "Confirmar compra";
}

let productsCache = [];
let cart = [];

function formatMoney(value) {
    return Number(value || 0).toFixed(2);
}

/**
 * Carga la lista de clientes activos desde GET /api/Customers (solo para Employee/Admin).
 */
async function loadCustomers() {
    try {
        const data = await apiClient.get("/Customers");
        const customers = Array.isArray(data) ? data : [];
        customers.forEach((c) => {
            const opt = document.createElement("option");
            opt.value = c.customerId;
            opt.textContent = `${c.customerId} - ${c.companyName}`;
            customerSelect.appendChild(opt);
        });
    } catch (error) {
        showToast(error.message || "No se pudo cargar la lista de clientes.", "warning");
    }
}

function renderProductsMessage(message) {
    productsTbody.replaceChildren();
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 5;
    td.className = "text-center text-muted py-4";
    td.textContent = message;
    tr.appendChild(td);
    productsTbody.appendChild(tr);
}

/**
 * Carga los productos disponibles desde GET /api/Products.
 */
async function loadProducts() {
    clearInlineAlert(pageAlert);
    renderProductsMessage("Cargando productos...");

    try {
        const data = await apiClient.get("/Products");
        productsCache = Array.isArray(data) ? data : [];
        renderProducts(productsCache);
    } catch (error) {
        renderProductsMessage("No fue posible cargar el catálogo de productos.");
        showInlineAlert(pageAlert, error.message || "Error al cargar el catálogo de productos.", "danger", error.errors);
    }
}

/**
 * Renderiza la tabla de productos del POS.
 * REGLA DE NEGOCIO EN UI: Si el producto está descontinuado o sin stock, el botón
 * "+ Agregar" queda deshabilitado (btnAdd.disabled = !available).
 */
function renderProducts(list) {
    productsTbody.replaceChildren();

    if (list.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 5;
        td.className = "text-center text-muted py-4";
        td.textContent = "No se encontraron productos.";
        tr.appendChild(td);
        productsTbody.appendChild(tr);
        return;
    }

    list.forEach((product) => {
        const stock = Number(product.unitsInStock || 0);
        const discontinued = Boolean(product.discontinued);
        const available = !discontinued && stock > 0;

        const tr = document.createElement("tr");

        const tdName = document.createElement("td");
        const nameDiv = document.createElement("div");
        nameDiv.className = "fw-semibold";
        nameDiv.textContent = product.productName;
        tdName.appendChild(nameDiv);
        if (product.quantityPerUnit) {
            const subDiv = document.createElement("div");
            subDiv.className = "small text-muted";
            subDiv.textContent = product.quantityPerUnit;
            tdName.appendChild(subDiv);
        }

        const tdPrice = document.createElement("td");
        tdPrice.className = "text-end";
        tdPrice.textContent = `$${formatMoney(product.unitPrice)}`;

        const tdStock = document.createElement("td");
        tdStock.className = "text-center";
        tdStock.textContent = stock;

        const tdStatus = document.createElement("td");
        tdStatus.className = "text-center";
        const badge = document.createElement("span");
        if (discontinued) {
            badge.className = "badge text-bg-secondary";
            badge.textContent = "Descontinuado";
        } else if (stock <= 0) {
            badge.className = "badge text-bg-danger";
            badge.textContent = "Sin stock";
        } else {
            badge.className = "badge text-bg-success";
            badge.textContent = "Disponible";
        }
        tdStatus.appendChild(badge);

        const tdAction = document.createElement("td");
        tdAction.className = "text-end";
        const btnAdd = document.createElement("button");
        btnAdd.type = "button";
        btnAdd.className = "btn btn-sm btn-outline-primary";
        btnAdd.textContent = "+ Agregar";
        btnAdd.disabled = !available;
        btnAdd.addEventListener("click", () => addToCart(product));
        tdAction.appendChild(btnAdd);

        tr.append(tdName, tdPrice, tdStock, tdStatus, tdAction);
        productsTbody.appendChild(tr);
    });
}

/**
 * Agrega un producto al carrito o incrementa su cantidad si ya estaba agregado,
 * evitando duplicar renglones del mismo producto y validando que no exceda el stock.
 */
function addToCart(product) {
    const stock = Number(product.unitsInStock || 0);
    if (Boolean(product.discontinued) || stock <= 0) {
        showToast("Este producto no está disponible para la venta.", "warning");
        return;
    }

    const existing = cart.find((item) => item.productId === product.productId);
    if (existing) {
        if (existing.quantity + 1 > stock) {
            showToast(`Solo hay ${stock} unidades disponibles de ${product.productName}.`, "warning");
            return;
        }
        existing.quantity += 1;
    } else {
        cart.push({
            productId: product.productId,
            productName: product.productName,
            unitPrice: Number(product.unitPrice || 0),
            maxStock: stock,
            quantity: 1
        });
    }

    renderCart();
}

// Ajusta la cantidad (+1 o -1) de un producto dentro del carrito:
// - Si baja a 0 o menos, elimina el renglón del carrito.
// - Si intenta superar el stock disponible (item.maxStock), muestra advertencia y lo bloquea.
function updateQuantity(productId, delta) {
    const item = cart.find((i) => i.productId === productId);
    if (!item) return;

    const nextQty = item.quantity + delta;
    if (nextQty <= 0) {
        removeFromCart(productId);
        return;
    }
    if (nextQty > item.maxStock) {
        showToast(`Stock máximo alcanzado (${item.maxStock}) para ${item.productName}.`, "warning");
        return;
    }

    item.quantity = nextQty;
    renderCart();
}

// Quita por completo un producto del arreglo 'cart' y vuelve a dibujar la tabla del carrito
function removeFromCart(productId) {
    cart = cart.filter((i) => i.productId !== productId);
    renderCart();
}

// Dibuja el estado actual del carrito en pos.html y calcula el total estimado en pantalla
// (recordando que el total y precios definitivos siempre los recalcula OrderService en el servidor).
function renderCart() {
    cartTbody.replaceChildren();

    if (cart.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 4;
        td.className = "text-center text-muted py-3";
        td.textContent = "El carrito está vacío.";
        tr.appendChild(td);
        cartTbody.appendChild(tr);
        cartTotal.textContent = "$0.00";
        btnConfirmSale.disabled = true;
        return;
    }

    let total = 0;

    cart.forEach((item) => {
        const subtotal = item.unitPrice * item.quantity;
        total += subtotal;

        const tr = document.createElement("tr");

        const tdName = document.createElement("td");
        tdName.textContent = item.productName;

        const tdQty = document.createElement("td");
        tdQty.className = "text-center";
        const group = document.createElement("div");
        group.className = "btn-group btn-group-sm";

        const btnMinus = document.createElement("button");
        btnMinus.type = "button";
        btnMinus.className = "btn btn-outline-secondary";
        btnMinus.textContent = "-";
        btnMinus.addEventListener("click", () => updateQuantity(item.productId, -1));

        const spanQty = document.createElement("span");
        spanQty.className = "btn btn-light disabled px-2";
        spanQty.textContent = item.quantity;

        const btnPlus = document.createElement("button");
        btnPlus.type = "button";
        btnPlus.className = "btn btn-outline-secondary";
        btnPlus.textContent = "+";
        btnPlus.addEventListener("click", () => updateQuantity(item.productId, 1));

        group.append(btnMinus, spanQty, btnPlus);
        tdQty.appendChild(group);

        const tdSub = document.createElement("td");
        tdSub.className = "text-end";
        tdSub.textContent = `$${formatMoney(subtotal)}`;

        const tdDel = document.createElement("td");
        tdDel.className = "text-end";
        const btnDel = document.createElement("button");
        btnDel.type = "button";
        btnDel.className = "btn btn-sm btn-outline-danger";
        btnDel.textContent = "×";
        btnDel.addEventListener("click", () => removeFromCart(item.productId));
        tdDel.appendChild(btnDel);

        tr.append(tdName, tdQty, tdSub, tdDel);
        cartTbody.appendChild(tr);
    });

    cartTotal.textContent = `$${formatMoney(total)}`;
    btnConfirmSale.disabled = false;
}

btnClearCart.addEventListener("click", async () => {
    if (cart.length === 0) return;
    const confirmed = await showConfirmModal({
        title: "Vaciar carrito",
        message: "¿Seguro que deseas eliminar todos los productos del carrito?",
        confirmText: "Vaciar",
        confirmVariant: "danger"
    });
    if (!confirmed) return;
    cart = [];
    renderCart();
});

/**
 * AQUÍ SE CONFIRMA LA VENTA (POST /api/Orders -> OrdersController.Create -> OrderService.CrearOrdenAsync):
 * - Observa que en 'payload' SOLO viajan productId y quantity (y customerId si vende un empleado/admin).
 * - El precio real se toma de 'products' en MySQL, el vendedor del JWT y el destino de 'customers' en MySQL.
 */
btnConfirmSale.addEventListener("click", async () => {
    clearInlineAlert(pageAlert);

    if (cart.length === 0) {
        showToast("Agrega al menos un producto al carrito.", "warning");
        return;
    }

    const payload = {
        items: cart.map((i) => ({
            productId: i.productId,
            quantity: i.quantity
        }))
    };

    if (isStaff) {
        const selectedCustomer = customerSelect.value;
        if (!selectedCustomer) {
            showToast("Selecciona un cliente para registrar la venta.", "warning");
            return;
        }
        payload.customerId = selectedCustomer;
    }

    btnConfirmSale.disabled = true;

    try {
        const data = await apiClient.post("/Orders", payload);
        const orderIdText = data?.orderId ? ` (#${data.orderId})` : "";
        showToast(`Venta registrada exitosamente${orderIdText}.`, "success");
        cart = [];
        if (isStaff) {
            customerSelect.value = "";
        }
        renderCart();
        await loadProducts();
    } catch (error) {
        showInlineAlert(pageAlert, error.message || "No se pudo registrar la venta.", "danger", error.errors);
        btnConfirmSale.disabled = false;
    }
});

searchInput.addEventListener("input", (e) => {
    const q = e.target.value.trim().toLowerCase();
    const filtered = productsCache.filter((p) =>
        (p.productName || "").toLowerCase().includes(q)
    );
    renderProducts(filtered);
});

renderCart();
if (isStaff) {
    loadCustomers();
}
loadProducts();
