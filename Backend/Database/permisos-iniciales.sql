-- Permisos del Backend (ver Services/PermisosSistema.cs) y su asignación a roles.
-- Idempotente: se puede ejecutar varias veces. No modifica el esquema.
--
-- 1) Revise los nombres de sus roles:   SELECT idRol, nombreRol FROM Roles;
-- 2) Ajuste los tres nombres de la sección CONFIGURACIÓN si difieren.
-- 3) Ejecute todo el script.

-- ---------------- Permisos ----------------
INSERT INTO Permisos (nombrePermiso)
SELECT v.nombrePermiso
FROM (VALUES
    ('usuarios.gestionar'),
    ('seguridad.gestionar'),
    ('kardex.modificar'),
    ('auditorias.consultar'),
    ('auditorias.modificar'),
    ('mesas.gestionar'),
    ('cuentas.gestionar'),
    ('pedidos.gestionar'),
    ('cobros.gestionar'),
    ('inventario.gestionar')
) AS v(nombrePermiso)
WHERE NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.nombrePermiso = v.nombrePermiso);

-- ---------------- CONFIGURACIÓN: nombres exactos de sus roles ----------------
DECLARE @nombreAdmin  NVARCHAR(100) = N'Administrador';
DECLARE @nombreCajero NVARCHAR(100) = N'Cajero';
DECLARE @nombreMesero NVARCHAR(100) = N'Mesero';

DECLARE @idAdmin INT  = (SELECT TOP 1 idRol FROM Roles WHERE nombreRol = @nombreAdmin);
DECLARE @idCajero INT = (SELECT TOP 1 idRol FROM Roles WHERE nombreRol = @nombreCajero);
DECLARE @idMesero INT = (SELECT TOP 1 idRol FROM Roles WHERE nombreRol = @nombreMesero);

IF @idAdmin IS NULL OR @idCajero IS NULL OR @idMesero IS NULL
    THROW 50000, 'Algun rol no existe: revise los nombres en la seccion CONFIGURACION.', 1;

-- ---------------- Asignación ----------------
-- Mesero: mesas, cuentas y pedidos.
-- Cajero: lo del mesero + cobros (comprobantes y métodos de pago).
-- Administrador: todos los permisos.
DECLARE @asignaciones TABLE (idRol INT, nombrePermiso NVARCHAR(100));

INSERT INTO @asignaciones (idRol, nombrePermiso) VALUES
    (@idMesero, N'mesas.gestionar'),
    (@idMesero, N'cuentas.gestionar'),
    (@idMesero, N'pedidos.gestionar'),
    (@idCajero, N'mesas.gestionar'),
    (@idCajero, N'cuentas.gestionar'),
    (@idCajero, N'pedidos.gestionar'),
    (@idCajero, N'cobros.gestionar');

INSERT INTO @asignaciones (idRol, nombrePermiso)
SELECT @idAdmin, nombrePermiso FROM Permisos;

INSERT INTO RolesPermisos (idRol, idPermiso)
SELECT a.idRol, p.idPermiso
FROM @asignaciones a
JOIN Permisos p ON p.nombrePermiso = a.nombrePermiso
WHERE NOT EXISTS (
    SELECT 1 FROM RolesPermisos rp
    WHERE rp.idRol = a.idRol AND rp.idPermiso = p.idPermiso);

-- ---------------- Verificación ----------------
SELECT r.nombreRol, p.nombrePermiso
FROM RolesPermisos rp
JOIN Roles r ON r.idRol = rp.idRol
JOIN Permisos p ON p.idPermiso = rp.idPermiso
ORDER BY r.nombreRol, p.nombrePermiso;
