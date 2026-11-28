-- =============================================================================
-- 005 · Costo promedio ponderado móvil en el Kardex (Fase 2)
-- Qué hace, en una sola transacción:
--   1. Agrega a Kardex:
--        costoUnitario decimal(12,2) NOT NULL DEFAULT 0 -> costo con que se valora el movimiento
--        costoPromedio decimal(12,2) NOT NULL DEFAULT 0 -> costo promedio del producto después del movimiento
--   2. Calcula ambas columnas para los movimientos existentes, en orden (producto, fecha, id):
--        ENTRADA de una compra ("Compra #N ..." o "Compra CMP-000N"): costo de esa compra y
--          promedio = (stock anterior × promedio + cantidad × costo) / (stock anterior + cantidad)
--        Cualquier otro movimiento: se valora al promedio vigente y no lo cambia.
--   3. Verifica que cada producto con una sola compra quede con el costo de esa compra y que el
--      valor del inventario coincida con el cálculo anterior; si no, deshace todo.
-- Modo:
--   'ENSAYO'     -> hace todo, muestra el resultado y DESHACE (ROLLBACK). Úselo primero.
--   'DEFINITIVO' -> hace todo y CONFIRMA (COMMIT).
-- ORDEN DE DESPLIEGUE: ejecute este script en DEFINITIVO ANTES de publicar el backend nuevo.
-- Requisito: respaldo .bacpac reciente. Base: ELQUATE (Azure SQL). Se puede ejecutar más de una vez
-- (recalcula todo el historial con la misma regla).
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
-- 1. Columnas nuevas
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Kardex', N'costoUnitario') IS NULL
    ALTER TABLE dbo.Kardex
        ADD costoUnitario decimal(12,2) NOT NULL CONSTRAINT DF_Kardex_costoUnitario DEFAULT (0);

IF COL_LENGTH(N'dbo.Kardex', N'costoPromedio') IS NULL
    ALTER TABLE dbo.Kardex
        ADD costoPromedio decimal(12,2) NOT NULL CONSTRAINT DF_Kardex_costoPromedio DEFAULT (0);

-- ---------------------------------------------------------------------------
-- 2 y 3. Cálculo y verificación (SQL dinámico: las columnas no existían al compilar el lote)
-- ---------------------------------------------------------------------------
EXEC sys.sp_executesql N'
    DECLARE @id int, @prod int, @tipo varchar(10), @cant int, @stockAnt int, @motivo nvarchar(500);
    DECLARE @prodActual int = -1, @prom decimal(18,6) = 0, @costo decimal(18,6), @idCompra int;

    DECLARE movimientos CURSOR LOCAL FAST_FORWARD FOR
        SELECT idMovimiento, idProducto, tipoMovimiento, cantidad, stockAnterior, motivo
        FROM dbo.Kardex
        ORDER BY idProducto, fecha, idMovimiento;

    OPEN movimientos;
    FETCH NEXT FROM movimientos INTO @id, @prod, @tipo, @cant, @stockAnt, @motivo;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @prod <> @prodActual
        BEGIN
            SET @prodActual = @prod;
            SET @prom = 0;
        END;

        SET @costo = NULL;
        SET @idCompra = NULL;

        IF @tipo = ''ENTRADA''
        BEGIN
            -- Compra referida en el motivo: "Compra #12 · ..." (carga de apertura) o "Compra CMP-000012" (aplicación)
            IF @motivo LIKE N''Compra #%''
                SET @idCompra = TRY_CAST(SUBSTRING(@motivo, 9, CHARINDEX(N'' '', @motivo + N'' '', 9) - 9) AS int);
            ELSE IF @motivo LIKE N''Compra CMP-%''
                SET @idCompra = TRY_CAST(SUBSTRING(@motivo, 12, 6) AS int);

            IF @idCompra IS NOT NULL
                SELECT @costo = SUM(dc.cantidad * dc.precioCompra) / NULLIF(SUM(dc.cantidad), 0)
                FROM dbo.DetalleCompras dc
                WHERE dc.idCompra = @idCompra AND dc.idProducto = @prod;
        END;

        IF @costo IS NOT NULL AND @cant > 0
        BEGIN
            SET @prom = CASE WHEN @stockAnt <= 0 THEN @costo
                             ELSE (@stockAnt * @prom + @cant * @costo) / (@stockAnt + @cant) END;
            UPDATE dbo.Kardex
            SET costoUnitario = ROUND(@costo, 2), costoPromedio = ROUND(@prom, 2)
            WHERE idMovimiento = @id;
            SET @prom = ROUND(@prom, 2);
        END
        ELSE
            UPDATE dbo.Kardex
            SET costoUnitario = ROUND(@prom, 2), costoPromedio = ROUND(@prom, 2)
            WHERE idMovimiento = @id;

        FETCH NEXT FROM movimientos INTO @id, @prod, @tipo, @cant, @stockAnt, @motivo;
    END;

    CLOSE movimientos;
    DEALLOCATE movimientos;

    -- Estado vigente de cada producto (último movimiento)
    ;WITH ultimo AS (
        SELECT k.idProducto, k.stockNuevo, k.costoPromedio,
               ROW_NUMBER() OVER (PARTITION BY k.idProducto ORDER BY k.fecha DESC, k.idMovimiento DESC) AS n
        FROM dbo.Kardex k
    ),
    compras AS (
        SELECT dc.idProducto, COUNT(*) AS lineas, AVG(dc.precioCompra) AS precioPromedio
        FROM dbo.DetalleCompras dc GROUP BY dc.idProducto
    )
    SELECT u.idProducto, u.stockNuevo, u.costoPromedio, c.lineas, c.precioPromedio
    INTO #vigente
    FROM ultimo u LEFT JOIN compras c ON c.idProducto = u.idProducto
    WHERE u.n = 1;

    -- V1. Entradas de compra cuyo costo no se encontró
    IF EXISTS (SELECT 1 FROM dbo.Kardex
               WHERE tipoMovimiento = ''ENTRADA'' AND (motivo LIKE N''Compra #%'' OR motivo LIKE N''Compra CMP-%'')
                 AND costoUnitario = 0)
    BEGIN
        SELECT idMovimiento, idProducto, motivo AS entrada_sin_costo
        FROM dbo.Kardex
        WHERE tipoMovimiento = ''ENTRADA'' AND (motivo LIKE N''Compra #%'' OR motivo LIKE N''Compra CMP-%'') AND costoUnitario = 0;
        THROW 50003, N''Hay entradas de compra sin costo (ver resultado). No se confirmó nada.'', 1;
    END;

    -- V2. Producto con una sola línea de compra: su promedio debe ser exactamente ese costo
    IF EXISTS (SELECT 1 FROM #vigente WHERE lineas = 1 AND costoPromedio <> ROUND(precioPromedio, 2))
    BEGIN
        SELECT idProducto, costoPromedio, precioPromedio AS costo_de_su_unica_compra
        FROM #vigente WHERE lineas = 1 AND costoPromedio <> ROUND(precioPromedio, 2);
        THROW 50004, N''El costo promedio no coincide con la compra (ver resultado). No se confirmó nada.'', 1;
    END;

    -- Resultado: valor del inventario con el costo nuevo y con el cálculo anterior (promedio simple)
    SELECT COUNT(*) AS productos,
           SUM(stockNuevo) AS unidades_en_bodega,
           SUM(stockNuevo * costoPromedio) AS valor_inventario_costo_promedio,
           SUM(stockNuevo * ISNULL(precioPromedio, 0)) AS valor_inventario_calculo_anterior
    FROM #vigente;

    SELECT TOP (15) v.idProducto, p.nombreProducto, v.stockNuevo AS stock, v.costoPromedio
    FROM #vigente v JOIN dbo.Productos p ON p.idProducto = v.idProducto
    ORDER BY v.stockNuevo * v.costoPromedio DESC;

    DROP TABLE #vigente;';

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
