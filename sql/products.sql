-- Productos de ejemplo.
-- Ejecutar después de que la aplicación haya creado la tabla "Products".

INSERT INTO "Products" ("Name", "Description", "Price", "Stock", "ImageUrl", "Category", "CreatedAt")
VALUES
('Laptop Lenovo', 'Laptop para estudio y trabajo.', 4500.00, 5, NULL, 'Electrónica', NOW()),
('Mouse inalámbrico', 'Mouse inalámbrico USB.', 85.00, 20, NULL, 'Accesorios', NOW()),
('Teclado mecánico', 'Teclado mecánico para escritorio.', 350.00, 10, NULL, 'Accesorios', NOW()),
('Monitor 24 pulgadas', 'Monitor Full HD de 24 pulgadas.', 1200.00, 7, NULL, 'Electrónica', NOW());
