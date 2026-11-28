# Backend.Tests · Pruebas de El Quate

Proyecto de pruebas xUnit del backend (ASP.NET Core 9). Vive **al lado** de `Backend/`, nunca dentro.

| Carpeta | Qué prueba | Técnica |
|---|---|---|
| `Controllers/` | Los 20 controladores: cada acción y cada camino (200/201, 400, 404, 409, 500, 401, 403) | Caja blanca, Moq |
| `Repositories/` | Los 19 repositorios contra EF Core InMemory y un "bar de prueba" (`Ayudas/EscenarioBar.cs`) | Fake (base en memoria) |
| `DTOs/` | Las reglas de validación de los 37 DTOs, en su borde y justo por fuera | Valores límite, partición |
| `Mappers/` | Los 18 mappers: cada campo llega a su destino | Unitaria |
| `Services/` | JWT, permisos, bloqueo de login, revocación, sello, manejador de errores | Unitaria |
| `Context/` | Tablas, llaves, relaciones (sin cascada), precisión del dinero, longitudes (los modelos) | Metadatos de EF |
| `Seguridad/` | Matriz de `[Authorize]`, `[RequierePermiso]` y límite de intentos por acción | Inspección (reflexión) |
| `Integration/` | API completa en memoria: salud, 401, 403, login real y validaciones del token | WebApplicationFactory |

Ejecutar: `ejecutar-pruebas.cmd` en la raíz (pruebas + informe de cobertura), o en Visual Studio **Prueba → Explorador de pruebas → Ejecutar todas**.

Prueba omitida a propósito: `KardexRegistrarMovimientoTests` (usa `sp_getapplock` de SQL Server, que la base en memoria no soporta; se cubre con Postman).
