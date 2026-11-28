-- =============================================================================
-- 006 · Cobro en una sola transacción (Fase 2, punto 2.6)
-- Qué hace, en una sola transacción:
--   1. Revisa que ninguna cuenta tenga más de un comprobante. Si hay duplicados, los muestra y
--      no cambia nada (hay que decidir cuál se conserva antes de seguir).
--   2. Agrega a Comprobantes:
--        baseGravable decimal(10,2) NOT NULL DEFAULT 0     -> total sin el INC
--        impuesto     decimal(10,2) NOT NULL DEFAULT 0     -> INC incluido en el total
--        tasaImpuesto decimal(5,4)  NOT NULL DEFAULT 0.08  -> tarifa con que se emitió
--   3. Calcula las tres columnas para los comprobantes existentes, con la misma regla del sistema:
--        base = total / 1,08 redondeado a pesos (mitades hacia arriba); impuesto = total - base
--   4. Crea el índice único UX_Comprobantes_idCuenta: la base rechaza un segundo comprobante
--      para la misma cuenta, pase lo que pase en la aplicación.
--   5. Verifica todo; si algo no cuadra, deshace.
-- Modo:
--   'ENSAYO'     -> hace todo, muestra el resultado y DESHACE (ROLLBACK). Úselo primero.
--   'DEFINITIVO' -> hace todo y CONFIRMA (COMMIT).
-- ORDEN DE DESPLIEGUE: ejecute este script en DEFINITIVO ANTES de arrancar el backend nuevo
-- (el backend nuevo lee y escribe las columnas nuevas).
-- Requisito: respaldo .bacpac reciente. Base: ELQUATE (Azure SQL). Se puede ejecutar más de una vez.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Modo varchar(10) = 'ENSAYO';   -- cambie a 'DEFINITIVO' solo cuando el ENSAYO salga bien

IF DB_NAME() <> N'ELQUATE'
    THROW 50001, N'Este script solo se ejecuta sobre la base ELQUATE.', 1;

IF @Modo NOT IN ('ENSAYO', 'DEFINITIVO')
    THROW 50002, N'@Modo debe ser ENSAYO o DEFINITIVO.', 1;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. Cuentas con más de un comprobante (impedirían el índice único)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT idCuenta FROM dbo.Comprobantes GROUP BY idCuenta HAVING COUNT(*) > 1)
BEGIN
    SELECT c.idCuenta, c.idComprobante, c.fecha, c.total, c.idMetodo
    FROM dbo.Comprobantes c
    WHERE c.idCuenta IN (SELECT idCuenta FROM dbo.Comprobantes GROUP BY idCuenta HAVING COUNT(*) > 1)
    ORDER BY c.idCuenta, c.idComprobante;
    THROW 50003, N'Hay cuentas con más de un comprobante (ver resultado). No se cambió nada.', 1;
END;

-- ---------------------------------------------------------------------------
-- 2. Columnas nuevas
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Comprobantes', N'baseGravable') IS NULL
    ALTER TABLE dbo.Comprobantes
        ADD baseGravable decimal(10,2) NOT NULL CONSTRAINT DF_Comprobantes_baseGravable DEFAULT (0);

IF COL_LENGTH(N'dbo.Comprobantes', N'impuesto') IS NULL
    ALTER TABLE dbo.Comprobantes
        ADD impuesto decimal(10,2) NOT NULL CONSTRAINT DF_Comprobantes_impuesto DEFAULT (0);

IF COL_LENGTH(N'dbo.Comprobantes', N'tasaImpuesto') IS NULL
    ALTER TABLE dbo.Comprobantes
        ADD tasaImpuesto decimal(5,4) NOT NULL CONSTRAINT DF_Comprobantes_tasaImpuesto DEFAULT (0.08);

-- ---------------------------------------------------------------------------
-- 3. Desglose de los comprobantes existentes y 5. verificación
--    (SQL dinámico: las columnas no existían al compilar el lote)
-- ---------------------------------------------------------------------------
EXEC sys.sp_executesql N'
    UPDATE dbo.Comprobantes
    SET baseGravable = ROUND(total / 1.08, 0),
        impuesto     = total - ROUND(total / 1.08, 0),
        tasaImpuesto = 0.08;

    -- V1. Base + impuesto debe sumar exactamente el total
    IF EXISTS (SELECT 1 FROM dbo.Comprobantes WHERE baseGravable + impuesto <> total)
    BEGIN
        SELECT idComprobante, total, baseGravable, impuesto
        FROM dbo.Comprobantes WHERE baseGravable + impuesto <> total;
        THROW 50004, N''Hay comprobantes cuya base + impuesto no suma el total (ver resultado). No se confirmó nada.'', 1;
    END;

    -- V2. El impuesto nunca es negativo ni mayor que el total
    IF EXISTS (SELECT 1 FROM dbo.Comprobantes WHERE impuesto < 0 OR impuesto > total)
        THROW 50005, N''Hay comprobantes con un impuesto fuera de rango. No se confirmó nada.'', 1;';

-- ---------------------------------------------------------------------------
-- 4. Índice único: una cuenta, un comprobante
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_Comprobantes_idCuenta' AND object_id = OBJECT_ID(N'dbo.Comprobantes'))
    CREATE UNIQUE INDEX UX_Comprobantes_idCuenta ON dbo.Comprobantes (idCuenta);

-- V3. El índice existe y es único
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'UX_Comprobantes_idCuenta' AND object_id = OBJECT_ID(N'dbo.Comprobantes')
                 AND is_unique = 1)
    THROW 50006, N'No quedó creado el índice único UX_Comprobantes_idCuenta. No se confirmó nada.', 1;

-- ---------------------------------------------------------------------------
-- Resultado
-- ---------------------------------------------------------------------------
EXEC sys.sp_executesql N'
    SELECT COUNT(*) AS comprobantes,
           ISNULL(SUM(total), 0) AS total_cobrado,
           ISNULL(SUM(baseGravable), 0) AS total_base,
           ISNULL(SUM(impuesto), 0) AS total_inc
    FROM dbo.Comprobantes;

    SELECT TOP (10) idComprobante, idCuenta, fecha, total, baseGravable, impuesto, tasaImpuesto
    FROM dbo.Comprobantes
    ORDER BY idComprobante DESC;';

-- Informativo (no bloquea): cuentas en estado inconsistente, para la limpieza de datos de prueba
SELECT cu.idCuenta, cu.estado, cu.total,
       CASE WHEN cu.estado = N'Cerrada' THEN N'Cerrada sin comprobante'
            ELSE N'Abierta con comprobante' END AS observacion
FROM dbo.Cuentas cu
WHERE (cu.estado = N'Cerrada' AND NOT EXISTS (SELECT 1 FROM dbo.Comprobantes c WHERE c.idCuenta = cu.idCuenta))
   OR (cu.estado = N'Abierta' AND EXISTS (SELECT 1 FROM dbo.Comprobantes c WHERE c.idCuenta = cu.idCuenta));

IF @Modo = 'DEFINITIVO'
BEGIN
    COMMIT TRANSACTION;
    PRINT N'DEFINITIVO: cambios confirmados.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'ENSAYO: todo salió bien y se deshizo. Cambie @Modo a DEFINITIVO y vuelva a ejecutar.';
END;
