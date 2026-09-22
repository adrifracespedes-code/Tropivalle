-- Si ya tienes los productos, solo actualiza imágenes y precios/categorías por nombre.

UPDATE "Products" SET "ImageUrl"='/images/nectar-1l-retornable.jpg', "Price"=90.00, "Category"='Nectar TropiValle',
  "Description"='Caja 12 unidades. Sabores Durazno y Manzana.'
WHERE "Name" ILIKE '%1 Litro%Retornable%';

UPDATE "Products" SET "ImageUrl"='/images/nectar-2l-descartable.jpg', "Price"=72.00, "Category"='Nectar TropiValle',
  "Description"='Paquete 6 unidades. Sabores Durazno y Manzana. Tumbo'
WHERE "Name" ILIKE '%2 Litro%Descartable%';

UPDATE "Products" SET "ImageUrl"='/images/nectar-300ml-retornable.jpg', "Price"=36.00, "Category"='Nectar TropiValle',
  "Description"='Caja 12 unidades. Sabores Durazno y Manzana.'
WHERE "Name" ILIKE '%300ml%Retornable%';

UPDATE "Products" SET "ImageUrl"='/images/nectar-330ml-descartable.jpg', "Price"=36.00, "Category"='Nectar TropiValle',
  "Description"='Caja 12 unidades. Sabores Durazno y Manzana.'
WHERE "Name" ILIKE '%330ml%Descartable%';

UPDATE "Products" SET "ImageUrl"='/images/nectar-620ml-retornable.jpg', "Price"=60.00, "Category"='Nectar TropiValle',
  "Description"='Caja 12 unidades. Sabores Durazno y Manzana.'
WHERE "Name" ILIKE '%620ml%Retornable%';

UPDATE "Products" SET "ImageUrl"='/images/nectar-630ml-descartable.jpg', "Price"=60.00, "Category"='Nectar TropiValle',
  "Description"='Paquete 12 unidades. Sabores Durazno y Manzana.'
WHERE "Name" ILIKE '%630ml%Descartable%';

UPDATE "Products" SET "ImageUrl"='/images/agua-2l.jpg', "Price"=30.00, "Category"='Agua de Mesa',
  "Description"='Paquete de 6 unidades. Agua tratada y purificada con la más alta tecnología.'
WHERE "Name" ILIKE '%Agua de Mesa 2 Litro%';

UPDATE "Products" SET "ImageUrl"='/images/agua-330ml.jpg', "Price"=24.00, "Category"='Agua de Mesa',
  "Description"='Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.'
WHERE "Name" ILIKE '%Agua de Mesa 330%';

UPDATE "Products" SET "ImageUrl"='/images/agua-630ml.jpg', "Price"=36.00, "Category"='Agua de Mesa',
  "Description"='Paquete de 12 unidades. Agua tratada y purificada con la más alta tecnología.'
WHERE "Name" ILIKE '%Agua de Mesa 630%';

UPDATE "Products" SET "ImageUrl"='/images/agua-botellon-20l.jpg', "Price"=12.00, "Category"='Agua de Mesa',
  "Description"='1 unidad. Agua tratada y purificada con la más alta tecnología.'
WHERE "Name" ILIKE '%botellón%' OR "Name" ILIKE '%botellon%';
