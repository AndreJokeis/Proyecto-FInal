// ============================================================================
// ARCHIVO: wwwroot/js/pages/productos.js
// PROPÓSITO: Catálogo de Productos con filtros y CRUD para Admin/Employee.
//            AQUÍ SE HACE:
//            1. Revisión de acceso por URL (requireRoles(["Customer", "Employee", "Admin"])).
//            2. Si el usuario es Customer, solo ve el catálogo disponible (sin botones
//               de crear, editar ni descontinuar).
//            3. Si es Admin o Employee (isStaff), muestra el botón "+ Nuevo producto",
//               la columna de "Acciones" y consulta también los productos descontinuados.
//            4. Carga Categorías (GET /api/Categories) y Proveedores (GET /api/Suppliers)
//               para los selectores y filtros.
//            5. Baja lógica de productos: al pulsar "Descontinuar", llama a
//               DELETE /api/Products/{id} (que en ProductService marca Discontinued = true
//               sin borrar el producto de MySQL). Al pulsar "Reactivar", llama a
//               PUT /api/Products/{id} con discontinued = false.
//            6. Renderiza todo con document.createElement y .textContent (anti-XSS).
// ============================================================================

import { requireRoles } from "../guards/authGuard.js";
import { renderNavbar } from "../components/navbar.js?v=2";
import { apiClient } from "../services/apiClient.js";
import {
    clearInlineAlert,
    showConfirmModal,
    showInlineAlert,
    showToast
} from "../utils/ui.js";

// REVISIÓN DE ACCESO POR URL
const user = requireRoles(["Customer", "Employee", "Admin"]);

if (user) {
    renderNavbar();

    const roleLower = (user.role || "").toLowerCase();
    const isStaff = roleLower === "admin" || roleLower === "employee";

    const tbody = document.getElementById("products-tbody");
    const countBadge = document.getElementById("products-count");
    const searchInput = document.getElementById("search-input");
    const categoryFilter = document.getElementById("category-filter");
    const statusFilter = document.getElementById("status-filter");
    const pageAlert = document.getElementById("page-alert");
    const modalAlert = document.getElementById("modal-alert");
    const thActions = document.getElementById("th-actions");
    const btnNew = document.getElementById("btn-new-product");
    const pageSubtitle = document.getElementById("page-subtitle");

    const modalEl = document.getElementById("productModal");
    const modalTitle = document.getElementById("productModalLabel");
    const productModal = new window.bootstrap.Modal(modalEl);

    const form = document.getElementById("product-form");
    const btnSave = document.getElementById("btn-save-product");

    // Campos del formulario que coinciden exactamente con Create/UpdateProductRequestDto
    const fields = {
        productName: document.getElementById("productName"),
        quantityPerUnit: document.getElementById("quantityPerUnit"),
        categoryId: document.getElementById("categoryId"),
        supplierId: document.getElementById("supplierId"),
        unitPrice: document.getElementById("unitPrice"),
        unitsInStock: document.getElementById("unitsInStock"),
        discontinued: document.getElementById("discontinued")
    };

    // Solo Admin y Employee pueden ver controles de creación y edición
    if (isStaff) {
        btnNew.classList.remove("d-none");
        thActions.classList.remove("d-none");
        pageSubtitle.textContent = "Administra productos, precios, existencias y estado de descontinuación.";
    }

    let allProducts = [];
    let categoriesMap = new Map();
    let suppliersMap = new Map();
    let editingProduct = null;

    function formatMoney(value) {
        return Number(value || 0).toFixed(2);
    }

    function toNullIfEmpty(value) {
        const trimmed = (value || "").trim();
        return trimmed.length > 0 ? trimmed : null;
    }

    function renderEmptyRow(message) {
        tbody.replaceChildren();
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = isStaff ? 8 : 7;
        td.className = "text-center text-muted py-4";
        td.textContent = message;
        tr.appendChild(td);
        tbody.appendChild(tr);
    }

    /**
     * Renderiza la tabla del catálogo de productos.
     * PREVENCIÓN XSS: Todos los textos provenientes de la API se insertan con .textContent.
     */
    function renderTable(list) {
        countBadge.textContent = `${list.length} producto${list.length === 1 ? "" : "s"}`;

        if (list.length === 0) {
            renderEmptyRow("No se encontraron productos con los filtros seleccionados.");
            return;
        }

        tbody.replaceChildren();

        list.forEach((p) => {
            const stock = Number(p.unitsInStock || 0);
            const discontinued = Boolean(p.discontinued);

            const tr = document.createElement("tr");
            if (discontinued) {
                tr.className = "table-secondary text-muted";
            }

            const tdId = document.createElement("td");
            const code = document.createElement("span");
            code.className = "badge text-bg-light border text-dark";
            code.textContent = `#${p.productId}`;
            tdId.appendChild(code);

            const tdName = document.createElement("td");
            const nameMain = document.createElement("div");
            nameMain.className = "fw-semibold";
            nameMain.textContent = p.productName || "";
            tdName.appendChild(nameMain);
            if (p.quantityPerUnit) {
                const sub = document.createElement("small");
                sub.className = "text-muted";
                sub.textContent = p.quantityPerUnit;
                tdName.appendChild(sub);
            }

            const tdCategory = document.createElement("td");
            tdCategory.textContent =
                p.categoryName ||
                categoriesMap.get(Number(p.categoryId)) ||
                "—";

            const tdSupplier = document.createElement("td");
            tdSupplier.textContent =
                p.supplierName ||
                suppliersMap.get(Number(p.supplierId)) ||
                "—";

            const tdPrice = document.createElement("td");
            tdPrice.className = "text-end fw-semibold";
            tdPrice.textContent = `$${formatMoney(p.unitPrice)}`;

            const tdStock = document.createElement("td");
            tdStock.className = "text-center";
            tdStock.textContent = String(stock);

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

            tr.append(tdId, tdName, tdCategory, tdSupplier, tdPrice, tdStock, tdStatus);

            if (isStaff) {
                const tdActions = document.createElement("td");
                tdActions.className = "text-end text-nowrap";

                const editBtn = document.createElement("button");
                editBtn.type = "button";
                editBtn.className = "btn btn-outline-primary btn-sm me-1";
                editBtn.textContent = "Editar";
                editBtn.addEventListener("click", () => openEditModal(p));

                const toggleBtn = document.createElement("button");
                toggleBtn.type = "button";
                toggleBtn.className = discontinued
                    ? "btn btn-outline-success btn-sm"
                    : "btn btn-outline-warning btn-sm";
                toggleBtn.textContent = discontinued ? "Reactivar" : "Descontinuar";
                toggleBtn.addEventListener("click", () => handleToggleDiscontinued(p));

                tdActions.append(editBtn, toggleBtn);
                tr.appendChild(tdActions);
            }

            tbody.appendChild(tr);
        });
    }

    // Filtra en memoria por texto, categoría seleccionada y estado (disponible / sin stock / descontinuado)
    function applyFilters() {
        const q = searchInput.value.trim().toLowerCase();
        const selectedCategory = categoryFilter.value;
        const selectedStatus = statusFilter.value;

        const filtered = allProducts.filter((p) => {
            const stock = Number(p.unitsInStock || 0);
            const discontinued = Boolean(p.discontinued);

            if (selectedCategory && String(p.categoryId ?? "") !== selectedCategory) {
                return false;
            }

            if (selectedStatus === "available" && (discontinued || stock <= 0)) {
                return false;
            }
            if (selectedStatus === "out_of_stock" && (discontinued || stock > 0)) {
                return false;
            }
            if (selectedStatus === "discontinued" && !discontinued) {
                return false;
            }

            if (!q) return true;

            const haystack = [
                p.productId,
                p.productName,
                p.quantityPerUnit,
                p.categoryName || categoriesMap.get(Number(p.categoryId)),
                p.supplierName || suppliersMap.get(Number(p.supplierId))
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();

            return haystack.includes(q);
        });

        renderTable(filtered);
    }

    /**
     * Consulta Categorías (GET /api/Categories) y Proveedores (GET /api/Suppliers)
     * para llenar los selectores del filtro y del modal.
     */
    async function loadLookups() {
        const [categoriesResult, suppliersResult] = await Promise.allSettled([
            apiClient.get("/Categories", { skipAuthRedirect: true }),
            apiClient.get("/Suppliers", { skipAuthRedirect: true })
        ]);

        if (categoriesResult.status === "fulfilled" && Array.isArray(categoriesResult.value)) {
            categoriesMap.clear();
            categoriesResult.value.forEach((cat) => {
                const id = Number(cat.categoryId);
                const name = cat.categoryName || `Categoría #${id}`;
                categoriesMap.set(id, name);

                const filterOpt = document.createElement("option");
                filterOpt.value = String(id);
                filterOpt.textContent = name;
                categoryFilter.appendChild(filterOpt);

                const formOpt = document.createElement("option");
                formOpt.value = String(id);
                formOpt.textContent = name;
                fields.categoryId.appendChild(formOpt);
            });
        }

        if (suppliersResult.status === "fulfilled" && Array.isArray(suppliersResult.value)) {
            suppliersMap.clear();
            suppliersResult.value.forEach((sup) => {
                const id = Number(sup.supplierId);
                const name = sup.companyName || `Proveedor #${id}`;
                suppliersMap.set(id, name);

                const formOpt = document.createElement("option");
                formOpt.value = String(id);
                formOpt.textContent = name;
                fields.supplierId.appendChild(formOpt);
            });
        }
    }

    /**
     * Consulta los productos desde GET /api/Products.
     * Si es staff (Admin/Employee), envía ?includeDiscontinued=true para poder administrarlos.
     */
    async function loadProducts() {
        clearInlineAlert(pageAlert);
        renderEmptyRow("Cargando productos...");

        try {
            const data = await apiClient.get(isStaff ? "/Products?includeDiscontinued=true" : "/Products");
            allProducts = Array.isArray(data) ? data : [];
            applyFilters();
        } catch (error) {
            renderEmptyRow("No fue posible cargar el catálogo de productos.");
            showInlineAlert(pageAlert, error.message, "danger", error.errors);
        }
    }

    function resetForm() {
        form.reset();
        form.classList.remove("was-validated");
        clearInlineAlert(modalAlert);
    }

    function openCreateModal() {
        editingProduct = null;
        resetForm();
        modalTitle.textContent = "Nuevo producto";
        fields.unitsInStock.value = "0";
        fields.discontinued.checked = false;
        productModal.show();
    }

    function openEditModal(product) {
        editingProduct = product;
        resetForm();
        modalTitle.textContent = `Editar producto (#${product.productId})`;

        fields.productName.value = product.productName || "";
        fields.quantityPerUnit.value = product.quantityPerUnit || "";
        fields.categoryId.value = product.categoryId != null ? String(product.categoryId) : "";
        fields.supplierId.value = product.supplierId != null ? String(product.supplierId) : "";
        fields.unitPrice.value = product.unitPrice != null ? String(product.unitPrice) : "";
        fields.unitsInStock.value = product.unitsInStock != null ? String(product.unitsInStock) : "0";
        fields.discontinued.checked = Boolean(product.discontinued);

        productModal.show();
    }

    /**
     * AQUÍ SE HACE LA BAJA LÓGICA / REACTIVACIÓN DE UN PRODUCTO:
     * - Si se va a descontinuar: llama a DELETE /api/Products/{id} -> ProductService.DescontinuarAsync.
     * - Si se va a reactivar: llama a PUT /api/Products/{id} con discontinued: false.
     */
    async function handleToggleDiscontinued(product) {
        const nextDiscontinued = !Boolean(product.discontinued);
        const actionLabel = nextDiscontinued ? "Descontinuar" : "Reactivar";

        const confirmed = await showConfirmModal({
            title: `${actionLabel} producto`,
            message: nextDiscontinued
                ? `¿Deseas marcar "${product.productName}" como descontinuado? No podrá venderse en el Punto de Venta, conservando su historial de ventas.`
                : `¿Deseas reactivar "${product.productName}" para permitir su venta nuevamente?`,
            confirmText: actionLabel,
            confirmVariant: nextDiscontinued ? "warning" : "success"
        });

        if (!confirmed) return;

        try {
            if (nextDiscontinued) {
                const res = await apiClient.delete(`/Products/${product.productId}`);
                showToast(res?.message || "Producto marcado como descontinuado.", "success");
            } else {
                const payload = {
                    productName: product.productName,
                    supplierId: product.supplierId ?? null,
                    categoryId: product.categoryId ?? null,
                    quantityPerUnit: product.quantityPerUnit ?? null,
                    unitPrice: Number(product.unitPrice || 0),
                    unitsInStock: Number(product.unitsInStock || 0),
                    discontinued: false
                };
                await apiClient.put(`/Products/${product.productId}`, payload);
                showToast("Producto reactivado correctamente.", "success");
            }
            await loadProducts();
        } catch (error) {
            showToast(error.message || "No se pudo actualizar el estado del producto.", "danger");
        }
    }

    // Envío del formulario para Crear (POST /api/Products) o Editar (PUT /api/Products/{id})
    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        clearInlineAlert(modalAlert);

        fields.productName.value = fields.productName.value.trim();
        form.classList.add("was-validated");
        if (!form.checkValidity()) return;

        // Validación preventiva en UI contra nombres de productos duplicados
        const nameLower = fields.productName.value.toLowerCase();
        const duplicate = allProducts.some(
            (p) =>
                (p.productName || "").trim().toLowerCase() === nameLower &&
                (!editingProduct || p.productId !== editingProduct.productId)
        );
        if (duplicate) {
            showInlineAlert(modalAlert, "Ya existe un producto registrado con ese nombre.", "warning");
            return;
        }

        // MÍNIMA EXPOSICIÓN: Solo viajan las propiedades definidas en Create/UpdateProductRequestDto
        const payload = {
            productName: fields.productName.value,
            quantityPerUnit: toNullIfEmpty(fields.quantityPerUnit.value),
            categoryId: fields.categoryId.value ? Number(fields.categoryId.value) : null,
            supplierId: fields.supplierId.value ? Number(fields.supplierId.value) : null,
            unitPrice: Number(fields.unitPrice.value),
            unitsInStock: Number(fields.unitsInStock.value),
            discontinued: fields.discontinued.checked
        };

        btnSave.disabled = true;
        btnSave.textContent = "Guardando...";

        try {
            if (editingProduct) {
                await apiClient.put(`/Products/${editingProduct.productId}`, payload);
                showToast("Producto actualizado correctamente.", "success");
            } else {
                await apiClient.post("/Products", payload);
                showToast("Producto registrado correctamente.", "success");
            }

            productModal.hide();
            await loadProducts();
        } catch (error) {
            const variant = error.status === 409 ? "warning" : "danger";
            showInlineAlert(modalAlert, error.message, variant, error.errors);
        } finally {
            btnSave.disabled = false;
            btnSave.textContent = "Guardar";
        }
    });

    if (isStaff) {
        btnNew.addEventListener("click", openCreateModal);
    }
    searchInput.addEventListener("input", applyFilters);
    categoryFilter.addEventListener("change", applyFilters);
    statusFilter.addEventListener("change", applyFilters);

    (async () => {
        await loadLookups();
        await loadProducts();
    })();
}
