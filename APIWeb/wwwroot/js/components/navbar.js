import { clearSession, getUser } from "../services/session.js";

const NAV_ITEMS = [
    { label: "Inicio", href: "index.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Catálogo", href: "productos.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Punto de Venta", href: "pos.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Mis Pedidos / Ventas", href: "ventas.html", roles: ["Customer", "Employee", "Admin"] },
    { label: "Clientes", href: "clientes.html", roles: ["Employee", "Admin"] },
    { label: "Empleados", href: "empleados.html", roles: ["Admin"] }
];

export function renderNavbar(containerId = "navbar-container") {
    const host = document.getElementById(containerId);
    const user = getUser();
    if (!host || !user) return;

    const currentPage = window.location.pathname.split("/").pop() || "index.html";
    const userRoleLower = (user.role || "").toLowerCase();

    host.replaceChildren();

    const nav = document.createElement("nav");
    nav.className = "navbar navbar-expand-lg navbar-dark bg-dark shadow-sm mb-4";

    const container = document.createElement("div");
    container.className = "container-fluid";

    const brand = document.createElement("a");
    brand.className = "navbar-brand fw-semibold";
    brand.href = "index.html";
    brand.textContent = "Northwind POS";

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

    NAV_ITEMS.filter((item) =>
        item.roles.some((r) => r.toLowerCase() === userRoleLower)
    ).forEach((item) => {
        const li = document.createElement("li");
        li.className = "nav-item";

        const link = document.createElement("a");
        link.className = `nav-link${currentPage === item.href ? " active fw-semibold" : ""}`;
        link.href = item.href;
        link.textContent = item.label;

        li.appendChild(link);
        ul.appendChild(li);
    });

    const rightBox = document.createElement("div");
    rightBox.className = "d-flex align-items-center gap-3";

    const userInfo = document.createElement("span");
    userInfo.className = "navbar-text text-light small";
    userInfo.textContent = `${user.email || user.name || "Usuario"} (${user.role})`;

    const logoutBtn = document.createElement("button");
    logoutBtn.type = "button";
    logoutBtn.className = "btn btn-outline-danger btn-sm";
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