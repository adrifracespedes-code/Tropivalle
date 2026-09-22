-- Crear tabla Sales (sin FK estricta para evitar fallos al insertar)
CREATE TABLE IF NOT EXISTS "Sales" (
    "Id" SERIAL PRIMARY KEY,
    "ProductId" integer NOT NULL,
    "ProductName" character varying(150) NOT NULL,
    "Category" character varying(100) NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "SoldAt" timestamp with time zone NOT NULL,
    "Notes" character varying(300) NULL
);

CREATE INDEX IF NOT EXISTS "IX_Sales_SoldAt" ON "Sales" ("SoldAt");
CREATE INDEX IF NOT EXISTS "IX_Sales_Category" ON "Sales" ("Category");
