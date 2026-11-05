using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Kardex;
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
/// Pruebas unitarias (caja blanca) de KardexController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class KardexControllerCrudTests
{
    private readonly Mock<IKardexRepository> repoMock = new();
    private readonly KardexController sut;

    public KardexControllerCrudTests()
    {
        sut = new KardexController(repoMock.Object, new Mock<Microsoft.AspNetCore.Authorization.IAuthorizationService>().Object, NullLogger<KardexController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetKardex ----------

    [Fact]
    public async Task GetKardex_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetKardex()).ReturnsAsync(new List<Kardex> { new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" }, new Kardex { idMovimiento = 2, motivo = "Corrección de conteo" } });

        var resultado = await sut.GetKardex();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetKardex_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetKardex()).ReturnsAsync(new List<Kardex>());

        var resultado = await sut.GetKardex();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetKardex_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetKardex()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetKardex();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetKardexById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetKardexById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetKardexById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetKardexById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetKardexById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetKardexById(99)).ReturnsAsync((Kardex?)null);

        var resultado = await sut.GetKardexById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetKardexById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });

        var resultado = await sut.GetKardexById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetKardexById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetKardexById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutKardex ----------

    [Fact]
    public async Task PutKardex_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.PutKardex(It.IsAny<Kardex>())).ReturnsAsync(true);

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutKardex(It.Is<Kardex>(x => x.idMovimiento == 1 && x.motivo == "Corrección de conteo")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutKardex_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = id, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetKardexById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutKardex_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetKardexById(99)).ReturnsAsync((Kardex?)null);

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 99, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutKardex(It.IsAny<Kardex>()), Times.Never);
    }

    [Fact]
    public async Task PutKardex_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("motivo", "El campo no es válido.");

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutKardex(It.IsAny<Kardex>()), Times.Never);
    }

    [Fact]
    public async Task PutKardex_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.PutKardex(It.IsAny<Kardex>())).ReturnsAsync(false);

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutKardex_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.PutKardex(It.IsAny<Kardex>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutKardex_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutKardex(new KardexActualizarDTO { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, fecha = new DateTime(2026, 11, 5), motivo = "Corrección de conteo" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteKardex ----------

    [Fact]
    public async Task DeleteKardex_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.DeleteKardex(It.IsAny<Kardex>())).ReturnsAsync(true);

        var resultado = await sut.DeleteKardex(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteKardex(It.Is<Kardex>(x => x.idMovimiento == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteKardex_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteKardex(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetKardexById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteKardex_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetKardexById(99)).ReturnsAsync((Kardex?)null);

        var resultado = await sut.DeleteKardex(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteKardex(It.IsAny<Kardex>()), Times.Never);
    }

    [Fact]
    public async Task DeleteKardex_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.DeleteKardex(It.IsAny<Kardex>())).ReturnsAsync(false);

        var resultado = await sut.DeleteKardex(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteKardex_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ReturnsAsync(new Kardex { idMovimiento = 1, motivo = "Compra a proveedor" });
        repoMock.Setup(r => r.DeleteKardex(It.IsAny<Kardex>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteKardex(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteKardex_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetKardexById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteKardex(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
