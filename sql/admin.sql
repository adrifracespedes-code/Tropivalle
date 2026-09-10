-- Ejecutar después de registrarse.
-- Reemplazá TU_EMAIL por el correo exacto usado en Register.

INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'TU_EMAIL'
  AND r."Name" = 'Admin'
ON CONFLICT DO NOTHING;

-- Verificación:
SELECT u."Email", r."Name" AS "Role"
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
WHERE u."Email" = 'TU_EMAIL';
