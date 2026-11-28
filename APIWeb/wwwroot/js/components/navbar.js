// ============================================================================
// ARCHIVO: wwwroot/js/components/navbar.js
// PROPÓSITO: Genera dinámicamente la barra de navegación superior según el rol
//            del usuario autenticado (leído desde el token JWT en session.js).
//            AQUÍ SE HACE PASO A PASO:
//            1. Define en NAV_ITEMS qué páginas puede ver cada rol:
//               - Customer: Inicio, Catálogo, Punto de Venta y Mis Pedidos.
//               - Employee: Inicio, Catálogo, Punto de Venta, Ventas y Clientes.
//               - Admin:    Todo lo anterior + Empleados (alta exclusiva).
//            2. Construye todos los elementos con document.createElement y .textContent
//               (nunca innerHTML) para impedir ataques XSS en el nombre del usuario.
//            3. Configura el botón "Cerrar sesión", que ejecuta clearSession() para
//               borrar el JWT de localStorage y regresar a login.html.
// ============================================================================

import { clearSession, getUser } from "../services/session.js";

// Definición de enlaces del menú y qué roles tienen permiso de ver cada uno
const NAV_ITEMS = [
    { label: "Inicio", href: "index.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Catálogo", href: "productos.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Punto de Venta", href: "pos.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Mis Pedidos", href: "ventas.html", roles: ["Customer"] },
    { label: "Ventas", href: "ventas.html", roles: ["Employee", "Admin"] },
    { label: "Clientes", href: "clientes.html", roles: ["Employee", "Admin"] },
    { label: "Empleados", href: "empleados.html", roles: ["Admin"] }
];

const ROLE_LABELS = {
    admin: "Administrador",
    employee: "Empleado",
    customer: "Cliente"
};

export function renderNavbar(containerId = "navbar-container") {
    const host = document.getElementById(containerId);
    const user = getUser(); // Obtiene el usuario desde el JWT guardado en session.js
    if (!host || !user) return;

    const currentPage = window.location.pathname.split("/").pop() || "index.html";
    const userRoleLower = (user.role || "").toLowerCase();

    host.replaceChildren();

    // Barra Bootstrap verde (bg-success) con texto claro (navbar-dark)
    const nav = document.createElement("nav");
    nav.className = "navbar navbar-expand-lg navbar-dark bg-success shadow-sm mb-4";

    const container = document.createElement("div");
    container.className = "container-fluid";

    const brand = document.createElement("a");
    brand.className = "navbar-brand fw-bold text-white";
    brand.href = "index.html";
    brand.textContent = "Fashion Roma";

    const toggler = document.createElement("button");
    toggler.className = "navbar-toggler";
    toggler.type = "button";
    toggler.setAttribute("data-bs-toggle", "collapse");
    toggler.setAttribute("data-bs-target", "#mainNavbar");
    toggler.setAttribute("aria-controls", "mainNavbar");
    toggler.setAttribute("aria-expanded", "false");
    toggler.setAttribute("aria-label", "Abrir menú");

    const togglerIcon = document.createElement("span");
    togglerIcon.className = "navbar-toggler-icon";
    toggler.appendChild(togglerIcon);

    const collapse = document.createElement("div");
    collapse.className = "collapse navbar-collapse";
    collapse.id = "mainNavbar";

    const ul = document.createElement("ul");
    ul.className = "navbar-nav me-auto mb-2 mb-lg-0";

    // Filtra las opciones del menú para mostrar únicamente las permitidas para el rol actual
    NAV_ITEMS.filter((item) =>
        item.roles.some((r) => r.toLowerCase() === userRoleLower)
    ).forEach((item) => {
        const li = document.createElement("li");
        li.className = "nav-item";

        const link = document.createElement("a");
        link.className = `nav-link${currentPage === item.href ? " active fw-semibold text-white" : " link-light"}`;
        link.href = item.href;
        link.textContent = item.label;

        li.appendChild(link);
        ul.appendChild(li);
    });

    const rightBox = document.createElement("div");
    rightBox.className = "d-flex align-items-center gap-3";

    // Muestra el nombre/correo del usuario y su rol usando textContent (anti-XSS)
    const roleLabel = ROLE_LABELS[userRoleLower] || user.role;
    const userInfo = document.createElement("span");
    userInfo.className = "navbar-text text-white small";
    userInfo.textContent = `${user.name || user.email || "Usuario"} (${roleLabel})`;

    // Botón para cerrar sesión: borra el JWT de localStorage y regresa a login.html
    const logoutBtn = document.createElement("button");
    logoutBtn.type = "button";
    logoutBtn.className = "btn btn-outline-light btn-sm";
    logoutBtn.textContent = "Cerrar sesión";
    logoutBtn.addEventListener("click", () => {
        clearSession();
        window.location.replace("login.html");
    });

    rightBox.append(userInfo, logoutBtn);
    collapse.append(ul, rightBox);
    container.append(brand, toggler, collapse);
    nav.appendChild(container);
    host.appendChild(nav);
}
