# TropiValle — Local y Supabase

## Local (Visual Studio / `dotnet run`)

1. `appsettings.Development.json` ya usa SQLite:
   ```json
   "ConnectionStrings": { "DefaultConnection": "Data Source=tropivalle-local.db" },
   "Database": { "Provider": "sqlite" }
   ```
2. Ejecuta F5. Se crea `tropivalle-local.db` automáticamente.
3. No necesitas PostgreSQL instalado.

## Supabase (producción / Render)

1. Variable de entorno en Render:
   ```
   CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=....pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xxx;Password=...;SSL Mode=Require;Trust Server Certificate=true
   ```
2. Opcional: `DATABASE_PROVIDER=postgres`
3. Imágenes globales (opcional):
   ```
   SUPABASE_URL=https://xxxx.supabase.co
   SUPABASE_SERVICE_ROLE_KEY=...
   STORAGE_BUCKET=product-images
   ```

## UI

- Catálogo: modo de vista **Cuadrícula** (antes Instaview) y **Lista**.
