using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Pruebas unitarias (caja blanca) de CuentasController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class CuentasControllerCrudTests
{
    private readonly Mock<ICuentasRepository> repoMock = new();
    private readonly CuentasController sut;

    public CuentasControllerCrudTests()
    {
        sut = new CuentasController(repoMock.Object, NullLogger<CuentasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetCuentas ----------

    [Fact]
    public async Task GetCuentas_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetCuentas(It.IsAny<string?>())).ReturnsAsync(new List<Cuentas> { new Cuentas { idCuenta = 1, estado = "Abierta" }, new Cuentas { idCuenta = 2, estado = "Cerrada" } });

        var resultado = await sut.GetCuentas();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCuentas_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetCuentas(It.IsAny<string?>())).ReturnsAsync(new List<Cuentas>());

        var resultado = await sut.GetCuentas();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCuentas_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetCuentas(It.IsAny<string?>())).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCuentas();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetCuentaById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCuentaById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetCuentaById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCuentasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCuentaById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetCuentasById(99)).ReturnsAsync((Cuentas?)null);

        var resultado = await sut.GetCuentaById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCuentaById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });

        var resultado = await sut.GetCuentaById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCuentaById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCuentaById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostCuenta ----------

    [Fact]
    public async Task PostCuenta_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostCuentas(It.IsAny<Cuentas>())).ReturnsAsync(true);

        var resultado = await sut.PostCuenta(new CuentaCrearDTO { idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Abierta" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostCuentas(It.Is<Cuentas>(x => x.estado == "Abierta")), Times.Once);
    }

    [Fact]
    public async Task PostCuenta_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostCuentas(It.IsAny<Cuentas>())).ReturnsAsync(false);

        var resultado = await sut.PostCuenta(new CuentaCrearDTO { idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Abierta" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostCuenta_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("estado", "El campo no es válido.");

        var resultado = await sut.PostCuenta(new CuentaCrearDTO { idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Abierta" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostCuentas(It.IsAny<Cuentas>()), Times.Never);
    }

    [Fact]
    public async Task PostCuenta_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostCuentas(It.IsAny<Cuentas>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostCuenta(new CuentaCrearDTO { idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Abierta" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostCuenta_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostCuentas(It.IsAny<Cuentas>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostCuenta(new CuentaCrearDTO { idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Abierta" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutCuenta ----------

    [Fact]
    public async Task PutCuenta_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.PutCuentas(It.IsAny<Cuentas>())).ReturnsAsync(true);

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutCuentas(It.Is<Cuentas>(x => x.idCuenta == 1 && x.estado == "Cerrada")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutCuenta_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = id, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCuentasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutCuenta_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetCuentasById(99)).ReturnsAsync((Cuentas?)null);

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 99, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutCuentas(It.IsAny<Cuentas>()), Times.Never);
    }

    [Fact]
    public async Task PutCuenta_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("estado", "El campo no es válido.");

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutCuentas(It.IsAny<Cuentas>()), Times.Never);
    }

    [Fact]
    public async Task PutCuenta_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.PutCuentas(It.IsAny<Cuentas>())).ReturnsAsync(false);

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutCuenta_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.PutCuentas(It.IsAny<Cuentas>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutCuenta_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutCuenta(new CuentaActualizarDTO { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5), total = 50000m, estado = "Cerrada" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteCuenta ----------

    [Fact]
    public async Task DeleteCuenta_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.DeleteCuentas(It.IsAny<Cuentas>())).ReturnsAsync(true);

        var resultado = await sut.DeleteCuenta(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteCuentas(It.Is<Cuentas>(x => x.idCuenta == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteCuenta_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteCuenta(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCuentasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCuenta_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetCuentasById(99)).ReturnsAsync((Cuentas?)null);

        var resultado = await sut.DeleteCuenta(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteCuentas(It.IsAny<Cuentas>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCuenta_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.DeleteCuentas(It.IsAny<Cuentas>())).ReturnsAsync(false);

        var resultado = await sut.DeleteCuenta(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteCuenta_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ReturnsAsync(new Cuentas { idCuenta = 1, estado = "Abierta" });
        repoMock.Setup(r => r.DeleteCuentas(It.IsAny<Cuentas>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteCuenta(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteCuenta_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetCuentasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteCuenta(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
