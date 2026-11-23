# AGENTS & TECH STACK SPECIFICATION

## Integrantes y Roles
- **Lead Tech & PM**: Asistente IA (Arquitectura, revisión técnica, seguridad, contratos API y definición de tareas).
- **Joseph**: Backend Developer (ASP.NET Core Web API, Entity Framework Core, MySQL, reglas de negocio y endpoints del POS).
- **Richy**: Backend Developer & QA (ASP.NET Core Web API, autenticación/autorización, seguridad, pruebas funcionales/integración y verificación pre-despliegue).
- **Jesús**: Frontend Developer (HTML5, Bootstrap 5, JavaScript modular, catálogo, flujo de punto de venta y consumo de API).
- **Luis**: Frontend Developer (HTML5, Bootstrap 5, JavaScript modular, vistas de autenticación, protección de rutas por URL, módulos administrativos y UX).

## Stack Tecnológico Inmutable
- **Backend**: ASP.NET Core Web API (.NET 8 LTS, C#).
- **Acceso a Datos (ORM)**: Entity Framework Core con proveedor `Pomelo.EntityFrameworkCore.MySql`.
- **Base de Datos**: MySQL 8.x sobre esquema `nwind` (Northwind + extensiones controladas de seguridad y eliminación lógica).
- **Arquitectura Backend**: Arquitectura por Capas (`Controllers` → `Services` → `Repositories / DbContext` + `DTOs`) con Inyección de Dependencias nativa de ASP.NET Core.
- **Autenticación y Seguridad**: JWT (JSON Web Tokens), hashing seguro de contraseñas (`PasswordHasher` / `BCrypt`), autorización basada en roles (RBAC: `Admin`, `Employee`, `Customer`), bloqueo tras 3 intentos fallidos y/o Captcha.
- **Frontend**: HTML5 semántico + Bootstrap 5 + JavaScript Vanilla modular (ES6+ y `fetch` API). Sin frameworks pesados externos; todo el equipo debe dominar cada línea escrita para la revisión.
- **Despliegue**: Publicación en IIS / IIS Express sobre Máquina Virtual de Azure con Windows.

## Reglas de Oro del Código
1. **Separación estricta de responsabilidades**: Cero lógica de negocio, cálculos de ventas o consultas directas a `DbContext` dentro de los `Controllers`; toda lógica vive en la capa `Services`.
2. **Principio de Mínima Exposición (API Limpia)**: Prohibido devolver entidades de Entity Framework directamente en los endpoints. Toda entrada y salida usa DTOs explícitos que transportan únicamente la información necesaria (nunca exponer `PasswordHash`, claves internas ni campos sobrantes). Solo deben existir los controladores y endpoints estrictamente necesarios para el sistema.
3. **Única Fuente de Verdad en el Servidor (Server-Side Truth)**: El backend NUNCA confía en precios, totales, descuentos ni roles enviados desde el navegador. Al registrar una venta, el precio unitario (`UnitPrice`) se consulta siempre desde MySQL (`products`), se valida que el producto no esté descontinuado (`Discontinued == 0`), se verifica stock disponible (`UnitsInStock`) y se impiden registros duplicados.
4. **Eliminación Lógica Obligatoria (Soft Delete)**: Prohibido ejecutar `DELETE` físico sobre clientes o registros que comprometan la integridad referencial con ventas históricas (`orders`). Toda baja de cliente se realiza mediante bandera lógica y los listados excluyen registros inactivos por defecto.
5. **Defensa en Profundidad (Seguridad Web)**:
   - Autorización obligatoria en API (`[Authorize(Roles = "...")]`) **y** validación de acceso por URL/rol en el Frontend.
   - Protección anti-Clickjacking obligatoria en todas las respuestas HTTP (`X-Frame-Options: DENY` y `Content-Security-Policy: frame-ancestors 'none'`).
   - Sanitización de entradas en backend y prevención estricta de XSS en frontend (prohibido inyectar datos no confiables con `innerHTML`; usar `textContent`).
   - Protección contra fuerza bruta en login mediante bloqueo por 3 intentos fallidos y/o validación Captcha.
   - Mensajes de error claros y adecuados para el usuario final; prohibido filtrar stack traces, queries SQL o detalles internos del servidor.
6. **Flujo Git y Convenciones**: Las ramas de Git y los prefijos de commits se redactan en inglés simple (`feature/...`, `fix/...`, `chore/...`, `docs/...`). Todo cambio se integra hacia `develop` mediante Pull Request revisado, y hacia `main` solo en entregas estables y probadas.
7. **INMUTABILIDAD DE LA BITÁCORA (`memory.md`)**: El archivo `Documentation/memory.md` es un registro histórico del proyecto. Queda ESTRICTAMENTE PROHIBIDO sobrescribir o eliminar Sprints, tareas pasadas o decisiones arquitectónicas. Todo avance nuevo (incluyendo Sprints futuros de compañeros o IA) debe AGREGARSE al final o actualizar las casillas de verificación (`[ ]` a `[x]`), sin borrar la historia previa.
