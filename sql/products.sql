-- Catálogo completo TropiValle (Néctar + Agua de Mesa)
-- Ejecutar en PostgreSQL/Supabase después de crear la tabla Products.
-- Recomendado: vaciar productos viejos antes.

DELETE FROM "Products";

INSERT INTO "Products" ("Name", "Description", "Price", "Stock", "ImageUrl", "Category", "CreatedAt")
VALUES
-- Nectar TropiValle
(
  'TropiValle 1 Litro Retornable',
  'Caja 12 unidades. Sabores Durazno y Manzana.',
  90.00, 100,
  '/images/nectar-1l-retornable.jpg',
  'Nectar TropiValle', NOW()
),
(
  'TropiValle 2 Litros Descartable',
  'Paquete 6 unidades. Sabores Durazno y Manzana. Tumbo',
  72.00, 100,
  '/images/nectar-2l-descartable.jpg',
  'Nectar TropiValle', NOW()
),
(
  'TropiValle 300ml Retornable',
  'Caja 12 unidades. Sabores Durazno y Manzana.',
  36.00, 100,
  '/images/nectar-300ml-retornable.jpg',
  'Nectar TropiValle', NOW()
),
(
  'TropiValle 330ml Descartable',
  'Caja 12 unidades. Sabores Durazno y Manzana.',
  36.00, 100,
  '/images/nectar-330ml-descartable.jpg',
  'Nectar TropiValle', NOW()
),
(
  'TropiValle 620ml Retornable',
  'Caja 12 unidades. Sabores Durazno y Manzana.',
  60.00, 100,
  '/images/nectar-620ml-retornable.jpg',
  'Nectar TropiValle', NOW()
),
(
  'TropiValle 630ml Descartable',
  'Paquete 12 unidades. Sabores Durazno y Manzana.',
  60.00, 100,
  '/images/nectar-630ml-descartable.jpg',
  'Nectar TropiValle', NOW()
),
-- Agua de Mesa
(
  'Agua de Mesa 2 Litros',
  'Paquete de 6 unidades. Agua tratada y purificada con la más alta tecnología.',
  30.00, 100,
  '/images/agua-2l.jpg',
  'Agua de Mesa', NOW()
),
(
  'Agua de Mesa 330ml',
  'Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.',
  24.00, 100,
  '/images/agua-330ml.jpg',
  'Agua de Mesa', NOW()
),
(
  'Agua de Mesa 630ml',
  'Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.',
  36.00, 100,
  '/images/agua-630ml.jpg',
  'Agua de Mesa', NOW()
),
(
  'Agua de Mesa botellón de 20 litros',
  '1 unidad. Agua tratada y purificada con la más alta tecnología.',
  12.00, 100,
  '/images/agua-botellon-20l.jpg',
  'Agua de Mesa', NOW()
);
