import { requireRoles } from "../guards/authGuard.js";
import { renderNavbar } from "../components/navbar.js";

const user = requireRoles(["Customer", "Employee", "Admin"]);

if (user) {
    renderNavbar();

    const titleEl = document.getElementById("welcome-title");
    const subtitleEl = document.getElementById("welcome-subtitle");
    const cardsContainer = document.getElementById("dashboard-cards");

    titleEl.textContent = `Bienvenido, ${user.name || user.email || "Usuario"}`;
    subtitleEl.textContent = `Sesión iniciada con rol: ${user.role}`;

    const MODULES = [
        {
            title: "Catálogo de Productos",
            desc: "Consulta disponibilidad, precios y administración del catálogo.",
            href: "productos.html",
            roles: ["Customer", "Employee", "Admin"]
        },
        {
            title: "Punto de Venta",
            desc: "Registra nuevas compras y ventas en tiempo real.",
            href: "pos.html",
            roles: ["Customer", "Employee", "Admin"]
        },
        {
            title: "Historial de Ventas",
            desc: "Consulta tus pedidos o el historial operativo del sistema.",
            href: "ventas.html",
            roles: ["Customer", "Employee", "Admin"]
        },
        {
            title: "Gestión de Clientes",
            desc: "Alta, edición y baja lógica de clientes.",
            href: "clientes.html",
            roles: ["Employee", "Admin"]
        },
        {
            title: "Alta de Empleados",
            desc: "Administración exclusiva de cuentas de empleados y administradores.",
            href: "empleados.html",
            roles: ["Admin"]
        }
    ];

    const userRoleLower = (user.role || "").toLowerCase();

    MODULES.filter((m) =>
        m.roles.some((r) => r.toLowerCase() === userRoleLower)
    ).forEach((mod) => {
        const col = document.createElement("div");
        col.className = "col-12 col-md-6 col-lg-4";

        const card = document.createElement("div");
        card.className = "card h-100 shadow-sm border-0";

        const body = document.createElement("div");
        body.className = "card-body d-flex flex-column";

        const h2 = document.createElement("h2");
        h2.className = "h5 card-title fw-semibold";
        h2.textContent = mod.title;

        const p = document.createElement("p");
        p.className = "card-text text-muted small flex-grow-1";
        p.textContent = mod.desc;

        const btn = document.createElement("a");
        btn.href = mod.href;
        btn.className = "btn btn-outline-primary btn-sm align-self-start";
        btn.textContent = "Abrir módulo";

        body.append(h2, p, btn);
        card.appendChild(body);
        col.appendChild(card);
        cardsContainer.appendChild(col);
    });
}