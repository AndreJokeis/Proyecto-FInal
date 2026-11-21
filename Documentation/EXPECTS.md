# Proyecto Final - Programación Web II

## Descripción
El proyecto está orientado a un punto de venta que implemente las funcionalidades de un CRUD, así como también incluye funcionalidades de seguridad, persistencia de datos, despliegue, etc. Este punto de venta está basado en la base de datos "Northwind" para MySQL, con el cual podemos manejar una gran cantidad de productos, empresas, etc. De igual manera es necesario trabajar con usuarios para poder ingresar al sistema, de igual modo es necesario que se manejen ciertos roles en cuanto a los usuarios.

### Tecnologías
El proyecto tiene que estar usar ASP.NET Core para la parte del Backend, para el lado Frontend estaremos usando HTML con Bootstrap y JavaScript. Por la parte del despliegue estaremos desplegando la aplicación mediante IIS EXPRESS en una máquina virtual de Azure con Windows. Para la persistencia de los datos estaremos MySQL.

### Requerimientos
- Autenticación (registro de clientes de manera autónoma).

- Registro de administradores y otros roles con más permisos (empleados solo los da de alta el administrador, registrado tambien en empleados).

- Registro de ventas y gestión de clientes (Eliminación lógica).

- Autorización tanto en la API como revisión de acceso por URL.

- Mensajes adecuados.

- Asegurarse que únicamente viaje la información necesaria en la API, así como tener únicamente los controladores y endpoints necesarios.

- Incorporar reglas de negocio que aseguren la integridad de los datos y permitan tener un manejo correcto de la aplicación, como evitar eliminación, evitar duplicados, falsificar los datos (comprar productos descontinuados, evitar tomar el precio de la página del usuario).

- Evitar clickjacking (evitar incorporación con IFRAME).

- Sanitización de entradas para evitar XSS.

- Captcha y/o 3 intentos de acceso.

- Publicar APP.

- FrontEnd sin restricción de tecnología solo asegurar que la manejan (en la revisión).