using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Compras;
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
/// Pruebas unitarias (caja blanca) de ComprasController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class ComprasControllerCrudTests
{
    private readonly Mock<IComprasRepository> repoMock = new();
    private readonly ComprasController sut;

    public ComprasControllerCrudTests()
    {
        sut = new ComprasController(repoMock.Object, NullLogger<ComprasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetCompras ----------

    [Fact]
    public async Task GetCompras_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetCompras()).ReturnsAsync(new List<Compras> { new Compras { idCompra = 1, total = 120000m }, new Compras { idCompra = 2, total = 150000m } });

        var resultado = await sut.GetCompras();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCompras_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetCompras()).ReturnsAsync(new List<Compras>());

        var resultado = await sut.GetCompras();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCompras_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetCompras()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCompras();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetCompraById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCompraById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetCompraById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCompraById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetComprasById(99)).ReturnsAsync((Compras?)null);

        var resultado = await sut.GetCompraById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCompraById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });

        var resultado = await sut.GetCompraById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCompraById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCompraById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostCompra ----------

    [Fact]
    public async Task PostCompra_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostCompras(It.IsAny<Compras>())).ReturnsAsync(true);

        var resultado = await sut.PostCompra(new CompraCrearDTO { idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostCompras(It.Is<Compras>(x => x.total == 120000m)), Times.Once);
    }

    [Fact]
    public async Task PostCompra_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostCompras(It.IsAny<Compras>())).ReturnsAsync(false);

        var resultado = await sut.PostCompra(new CompraCrearDTO { idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostCompra_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("total", "El campo no es válido.");

        var resultado = await sut.PostCompra(new CompraCrearDTO { idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostCompras(It.IsAny<Compras>()), Times.Never);
    }

    [Fact]
    public async Task PostCompra_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostCompras(It.IsAny<Compras>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostCompra(new CompraCrearDTO { idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostCompras(It.IsAny<Compras>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostCompra(new CompraCrearDTO { idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutCompra ----------

    [Fact]
    public async Task PutCompra_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.PutCompras(It.IsAny<Compras>())).ReturnsAsync(true);

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutCompras(It.Is<Compras>(x => x.idCompra == 1 && x.total == 150000m)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutCompra_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = id, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutCompra_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetComprasById(99)).ReturnsAsync((Compras?)null);

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 99, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutCompras(It.IsAny<Compras>()), Times.Never);
    }

    [Fact]
    public async Task PutCompra_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("total", "El campo no es válido.");

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutCompras(It.IsAny<Compras>()), Times.Never);
    }

    [Fact]
    public async Task PutCompra_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.PutCompras(It.IsAny<Compras>())).ReturnsAsync(false);

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutCompra_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.PutCompras(It.IsAny<Compras>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutCompra(new CompraActualizarDTO { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 150000m });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteCompra ----------

    [Fact]
    public async Task DeleteCompra_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.DeleteCompras(It.IsAny<Compras>())).ReturnsAsync(true);

        var resultado = await sut.DeleteCompra(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteCompras(It.Is<Compras>(x => x.idCompra == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteCompra_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteCompra(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCompra_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetComprasById(99)).ReturnsAsync((Compras?)null);

        var resultado = await sut.DeleteCompra(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteCompras(It.IsAny<Compras>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCompra_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.DeleteCompras(It.IsAny<Compras>())).ReturnsAsync(false);

        var resultado = await sut.DeleteCompra(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteCompra_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ReturnsAsync(new Compras { idCompra = 1, total = 120000m });
        repoMock.Setup(r => r.DeleteCompras(It.IsAny<Compras>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteCompra(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetComprasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteCompra(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
