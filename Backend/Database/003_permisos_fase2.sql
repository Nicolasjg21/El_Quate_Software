-- =============================================================================
-- 003 · Permisos de la Fase 2
-- Qué hace:
--   1. Asegura que existan los permisos que ahora exige el backend
--      (cobros.gestionar, inventario.gestionar, mesas.gestionar).
--   2. Se los asigna al rol Administrador si le falta alguno.
--   3. Quita mesas.gestionar al rol Cajero: crear, eliminar, renumerar, retirar y
--      reactivar mesas queda solo para el Administrador (decisión del 25-nov-2026).
-- No toca usuarios, ventas ni inventario. Se puede ejecutar más de una vez.
-- Base: ELQUATE (Azure SQL). Todo o nada (transacción).
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ELQUATE'
    THROW 50001, N'Este script solo se ejecuta sobre la base ELQUATE.', 1;

BEGIN TRANSACTION;

-- 1. Permisos que exige el backend
INSERT INTO Permisos (nombrePermiso)
SELECT v.nombrePermiso
FROM (VALUES (N'cobros.gestionar'), (N'inventario.gestionar'), (N'mesas.gestionar')) AS v(nombrePermiso)
WHERE NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.nombrePermiso = v.nombrePermiso);

-- 2. El Administrador los tiene todos
INSERT INTO RolesPermisos (idRol, idPermiso)
SELECT r.idRol, p.idPermiso
FROM Roles r
JOIN Permisos p ON p.nombrePermiso IN (N'cobros.gestionar', N'inventario.gestionar', N'mesas.gestionar')
WHERE r.nombreRol = N'Administrador'
  AND NOT EXISTS (SELECT 1 FROM RolesPermisos rp WHERE rp.idRol = r.idRol AND rp.idPermiso = p.idPermiso);

-- 3. El Cajero ya no administra mesas (sigue ocupándolas, liberándolas y cobrando)
DELETE rp
FROM RolesPermisos rp
JOIN Roles r ON r.idRol = rp.idRol
JOIN Permisos p ON p.idPermiso = rp.idPermiso
WHERE r.nombreRol = N'Cajero' AND p.nombrePermiso = N'mesas.gestionar';

-- Verificación: si algo no quedó como se espera, se deshace todo
IF NOT EXISTS (SELECT 1 FROM RolesPermisos rp JOIN Roles r ON r.idRol = rp.idRol JOIN Permisos p ON p.idPermiso = rp.idPermiso
               WHERE r.nombreRol = N'Administrador' AND p.nombrePermiso = N'mesas.gestionar')
    THROW 50002, N'El Administrador quedó sin mesas.gestionar.', 1;

IF EXISTS (SELECT 1 FROM RolesPermisos rp JOIN Roles r ON r.idRol = rp.idRol JOIN Permisos p ON p.idPermiso = rp.idPermiso
           WHERE r.nombreRol = N'Cajero' AND p.nombrePermiso = N'mesas.gestionar')
    THROW 50003, N'El Cajero todavía tiene mesas.gestionar.', 1;

IF NOT EXISTS (SELECT 1 FROM RolesPermisos rp JOIN Roles r ON r.idRol = rp.idRol JOIN Permisos p ON p.idPermiso = rp.idPermiso
               WHERE r.nombreRol = N'Cajero' AND p.nombrePermiso = N'cobros.gestionar')
    THROW 50004, N'El Cajero no tiene cobros.gestionar: no podría cobrar.', 1;

COMMIT TRANSACTION;

-- Resultado final por rol
SELECT r.idRol, r.nombreRol, p.nombrePermiso
FROM Roles r
LEFT JOIN RolesPermisos rp ON rp.idRol = r.idRol
LEFT JOIN Permisos p ON p.idPermiso = rp.idPermiso
WHERE p.nombrePermiso LIKE N'%.%' OR p.nombrePermiso IS NULL
ORDER BY r.idRol, p.nombrePermiso;
