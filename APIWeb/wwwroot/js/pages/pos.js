import { renderNavbar } from "../components/navbar.js";
import { requireRoles } from "../guards/authGuard.js";
import { apiClient } from "../services/apiClient.js";
import { clearInlineAlert, showConfirmModal, showInlineAlert, showToast } from "../utils/ui.js";

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

function removeFromCart(productId) {
    cart = cart.filter((i) => i.productId !== productId);
    renderCart();
}

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
        renderCart();
        await loadProducts();
    } catch (error) {
        if (error.status === 404 || error.status === 405) {
            showInlineAlert(
                pageAlert,
                "El endpoint POST /api/Orders aún no está disponible en el backend.",
                "info"
            );
        } else {
            showInlineAlert(pageAlert, error.message || "No se pudo registrar la venta.", "danger", error.errors);
        }
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