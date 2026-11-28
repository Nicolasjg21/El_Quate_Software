using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// KardexRepository.RegistrarMovimiento NO se puede probar con EF InMemory, y se deja constancia aquí:
/// usa una transacción real y el bloqueo de SQL Server sp_getapplock (ExecuteSqlInterpolatedAsync),
/// que el proveedor en memoria no soporta. Sus reglas (ENTRADA suma, SALIDA resta, AJUSTE fija el valor,
/// no permite stock negativo) se verifican:
///   · en las pruebas del controlador (KardexMovimientosTests), con el repositorio simulado; y
///   · contra la base real, con la colección de Postman (pruebas de caja negra).
/// Para automatizarla haría falta un SQL Server de pruebas (por ejemplo, Testcontainers con Docker).
/// </summary>
public class KardexRegistrarMovimientoTests
{
    [Fact(Skip = "Requiere SQL Server real (sp_getapplock y transacción); se cubre con Postman y con KardexMovimientosTests.")]
    public void RegistrarMovimiento_RequiereSqlServer() { }
}
