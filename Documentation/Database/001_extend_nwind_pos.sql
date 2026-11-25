USE nwind_pos;

-- Campos necesarios para autenticacion local, roles y bloqueo de cuentas.
ALTER TABLE usuarios
	ADD COLUMN Rol VARCHAR(20) NOT NULL DEFAULT 'Customer' AFTER PasswordHash,
	ADD COLUMN FailedLoginAttempts INT UNSIGNED NOT NULL DEFAULT 0 AFTER Rol,
	ADD COLUMN LockoutEnd DATETIME(6) NULL AFTER FailedLoginAttempts,
	ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1 AFTER LockoutEnd,
	ADD COLUMN CustomerID VARCHAR(5)
		CHARACTER SET utf8mb3
		COLLATE utf8mb3_general_ci
		NULL AFTER IsActive,
	ADD COLUMN EmployeeID INT NULL AFTER CustomerID;

-- Una cuenta como maximo por cliente o empleado.
ALTER TABLE usuarios
	ADD UNIQUE KEY UX_usuarios_CustomerID (CustomerID),
	ADD UNIQUE KEY UX_usuarios_EmployeeID (EmployeeID),
	ADD CONSTRAINT FK_usuarios_customers
		FOREIGN KEY (CustomerID)
		REFERENCES customers (CustomerID)
		ON DELETE SET NULL,
	ADD CONSTRAINT FK_usuarios_employees
		FOREIGN KEY (EmployeeID)
		REFERENCES employees (EmployeeID)
		ON DELETE SET NULL;

-- Baja logica de clientes para conservar el historial de ventas.
ALTER TABLE customers
	ADD COLUMN IsDeleted TINYINT(1) NOT NULL DEFAULT 0 AFTER ImageThumbnail;

-- Evitar que se borren ordenes al eliminar fisicamente un cliente.
ALTER TABLE orders
	DROP FOREIGN KEY orders_ibfk_1,
	ADD CONSTRAINT FK_orders_customers_restrict
		FOREIGN KEY (CustomerID)
		REFERENCES customers (CustomerID)
		ON DELETE RESTRICT;

-- Evitar que se borren detalles historicos al eliminar fisicamente un producto.
ALTER TABLE `order details`
	DROP FOREIGN KEY `order details_ibfk_2`,
	ADD CONSTRAINT FK_order_details_products_restrict
		FOREIGN KEY (ProductID)
		REFERENCES products (ProductID)
		ON DELETE RESTRICT;

-- Verificacion de la estructura modificada.
SHOW CREATE TABLE usuarios;
SHOW CREATE TABLE customers;
SHOW CREATE TABLE orders;
SHOW CREATE TABLE `order details`;
