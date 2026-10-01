-- Permisos usados por la autorización del Backend (ver Services/PermisosSistema.cs).
-- Idempotente: solo inserta los que no existen. No modifica el esquema.
-- Ajuste los IDs de rol a su base de datos antes de ejecutar la parte de asignación.

INSERT INTO Permisos (nombrePermiso)
SELECT v.nombrePermiso
FROM (VALUES
    ('usuarios.gestionar'),
    ('seguridad.gestionar'),
    ('kardex.modificar'),
    ('auditorias.consultar'),
    ('auditorias.modificar')
) AS v(nombrePermiso)
WHERE NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.nombrePermiso = v.nombrePermiso);

-- Ejemplo: conceder todos estos permisos al rol 1 (reemplace 1 por el idRol administrador).
-- INSERT INTO RolesPermisos (idRol, idPermiso)
-- SELECT 1, p.idPermiso
-- FROM Permisos p
-- WHERE p.nombrePermiso IN ('usuarios.gestionar','seguridad.gestionar','kardex.modificar','auditorias.consultar','auditorias.modificar')
--   AND NOT EXISTS (SELECT 1 FROM RolesPermisos rp WHERE rp.idRol = 1 AND rp.idPermiso = p.idPermiso);
