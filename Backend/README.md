# El Quate Software – Backend

API ASP.NET Core 9 + EF Core (SQL Server) del sistema de gestión de El Cuate.

## Configuración (no se guardan secretos en el repositorio)

| Variable de entorno | Descripción |
|---|---|
| `ConnectionStrings__SQLConnectionStrings` | Cadena de conexión a SQL Server (obligatoria) |
| `Jwt__Key` | Clave HS256, mínimo 32 bytes (obligatoria) |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` | Emisor, audiencia y vigencia del token |
| `Cors__AllowedOrigins__0` (`__1`, …) | Orígenes del Frontend permitidos (obligatorio fuera de Development) |
| `Authorization__AdminRoleIds__0` | Opcional: rol de emergencia que omite la comprobación de permisos |
| `ForwardedHeaders__Habilitado` | `true` solo si hay un proxy inverso de confianza delante |

En desarrollo use `dotnet user-secrets`.

## Autorización

Se basa en los permisos de la base de datos (`Roles` → `RolesPermisos` → `Permisos`), con caché de 60 s.
Permisos (solo las acciones que modifican datos; las consultas GET requieren únicamente estar autenticado):

| Permiso | Protege | Mesero | Cajero | Admin |
|---|---|---|---|---|
| `mesas.gestionar`, `cuentas.gestionar`, `pedidos.gestionar` | Mesas, Cuentas, Pedidos, DetallePedidos | sí | sí | sí |
| `cobros.gestionar` | Comprobantes, MetodosPago | no | sí | sí |
| `inventario.gestionar` | Productos, Categorias, Proveedores, Compras, DetalleCompras | no | no | sí |
| `usuarios.gestionar`, `seguridad.gestionar`, `kardex.modificar`, `auditorias.*` | Usuarios, Roles/Permisos, Kardex (Put/Delete), Auditorías | no | no | sí |
 Cargue `Database/permisos-iniciales.sql` y asígnelos al rol administrador
**antes** de desplegar, o esos endpoints devolverán 403.

## Sesiones

El token incluye un sello derivado de la contraseña y un id de sesión: cambiar la contraseña, desactivar al usuario,
cambiar su rol o hacer `POST api/Autenticador/Logout` invalida el token. Logout y bloqueo de login (5 fallos / 15 min)
viven en memoria del proceso: con varias instancias deben migrarse a una caché distribuida. No hay refresh token.

`GET /health` (anónimo) sirve para sondas de disponibilidad.
