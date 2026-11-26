-- =============================================================================
-- 004 · Usuarios (Fase 2, bloque 2.12)
-- Qué hace, en una sola transacción:
--   1. Agrega a Usuarios:
--        debeCambiarPassword   bit NOT NULL DEFAULT 0  -> true mientras tenga una clave temporal
--        passwordTemporalExpira datetime2(0) NULL      -> vencimiento de la clave temporal (UTC)
--   2. Garantiza que el correo sea único con el índice UX_Usuarios_email.
--      Antes verifica que no haya correos vacíos ni repetidos; si los hay, se detiene sin cambiar
--      nada y los muestra. Ajusta la columna email a 254 caracteres, el límite que valida la
--      aplicación (hoy es varchar(100): un correo de más de 100 caracteres fallaría al guardar).
-- Modo:
--   'ENSAYO'     -> hace todo, muestra el resultado y DESHACE (ROLLBACK). Úselo primero.
--   'DEFINITIVO' -> hace todo y CONFIRMA (COMMIT).
-- ORDEN DE DESPLIEGUE: ejecute este script en DEFINITIVO ANTES de publicar el backend nuevo.
-- El backend anterior ignora las columnas nuevas; el nuevo falla si no existen.
-- Requisito: respaldo .bacpac reciente. Base: ELQUATE (Azure SQL). Se puede ejecutar más de una vez.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Modo varchar(10) = 'DEFINITIVO';   
IF DB_NAME() <> N'ELQUATE'
    THROW 50001, N'Este script solo se ejecuta sobre la base ELQUATE.', 1;

IF @Modo NOT IN ('ENSAYO', 'DEFINITIVO')
    THROW 50002, N'@Modo debe ser ENSAYO o DEFINITIVO.', 1;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. Columnas nuevas
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Usuarios', N'debeCambiarPassword') IS NULL
    ALTER TABLE dbo.Usuarios
        ADD debeCambiarPassword bit NOT NULL
            CONSTRAINT DF_Usuarios_debeCambiarPassword DEFAULT (0);

IF COL_LENGTH(N'dbo.Usuarios', N'passwordTemporalExpira') IS NULL
    ALTER TABLE dbo.Usuarios
        ADD passwordTemporalExpira datetime2(0) NULL;

-- ---------------------------------------------------------------------------
-- 2. Correo único
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Usuarios_email' AND object_id = OBJECT_ID(N'dbo.Usuarios'))
BEGIN
    -- 2a. Sin correos vacíos
    IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE email IS NULL OR LTRIM(RTRIM(email)) = N'')
    BEGIN
        SELECT idUsuario, nombres, apellidos, email AS correo_vacio
        FROM dbo.Usuarios WHERE email IS NULL OR LTRIM(RTRIM(email)) = N'';
        THROW 50003, N'Hay usuarios sin correo (ver resultado). Corríjalos desde la aplicación y vuelva a ejecutar.', 1;
    END;

    -- 2b. Sin correos repetidos (la comparación usa la intercalación de la columna, como el índice)
    IF EXISTS (SELECT 1 FROM dbo.Usuarios GROUP BY LTRIM(RTRIM(email)) HAVING COUNT(*) > 1)
    BEGIN
        SELECT u.idUsuario, u.nombres, u.apellidos, u.email AS correo_repetido, u.estado
        FROM dbo.Usuarios u
        WHERE LTRIM(RTRIM(u.email)) IN (SELECT LTRIM(RTRIM(email)) FROM dbo.Usuarios GROUP BY LTRIM(RTRIM(email)) HAVING COUNT(*) > 1)
        ORDER BY u.email, u.idUsuario;
        THROW 50004, N'Hay correos repetidos (ver resultado). Corríjalos desde la aplicación y vuelva a ejecutar.', 1;
    END;

    -- 2c. Espacios sobrantes (la aplicación ahora guarda el correo sin espacios)
    UPDATE dbo.Usuarios SET email = LTRIM(RTRIM(email)) WHERE email <> LTRIM(RTRIM(email));

    -- 2d. La columna debe tener exactamente el límite de la aplicación (254): más corta, un correo
    --     válido fallaría al guardar; (max), no admitiría índice.
    DECLARE @tipo sysname, @longitud smallint;
    SELECT @tipo = t.name, @longitud = c.max_length
    FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Usuarios') AND c.name = N'email';

    IF @longitud = -1 OR (@tipo = N'nvarchar' AND @longitud <> 508) OR (@tipo = N'varchar' AND @longitud <> 254)
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE LEN(email) > 254)
            THROW 50005, N'Hay correos de más de 254 caracteres; corríjalos antes de continuar.', 1;

        DECLARE @sql nvarchar(400) = N'ALTER TABLE dbo.Usuarios ALTER COLUMN email '
            + CASE WHEN @tipo = N'varchar' THEN N'varchar(254)' ELSE N'nvarchar(254)' END + N' NOT NULL;';
        EXEC sys.sp_executesql @sql;
    END;

    EXEC sys.sp_executesql N'CREATE UNIQUE NONCLUSTERED INDEX UX_Usuarios_email ON dbo.Usuarios (email);';
END;

-- ---------------------------------------------------------------------------
-- Verificación: si algo no quedó como se espera, se deshace todo
-- ---------------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Usuarios', N'debeCambiarPassword') IS NULL
   OR COL_LENGTH(N'dbo.Usuarios', N'passwordTemporalExpira') IS NULL
    THROW 50006, N'No se crearon las columnas nuevas.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Usuarios_email' AND object_id = OBJECT_ID(N'dbo.Usuarios') AND is_unique = 1)
    THROW 50007, N'No se creó el índice único de correo.', 1;

EXEC sys.sp_executesql N'
    IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE debeCambiarPassword = 1)
        THROW 50008, N''Ningún usuario debería quedar con contraseña temporal pendiente tras crear la columna.'', 1;';

-- Resultado (las columnas nuevas se leen con SQL dinámico porque no existían al compilar el lote)
EXEC sys.sp_executesql N'
    SELECT idUsuario, nombres, apellidos, email, estado, debeCambiarPassword, passwordTemporalExpira
    FROM dbo.Usuarios ORDER BY idUsuario;';

SELECT name AS indice, is_unique AS es_unico
FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuarios') AND name = N'UX_Usuarios_email';

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
