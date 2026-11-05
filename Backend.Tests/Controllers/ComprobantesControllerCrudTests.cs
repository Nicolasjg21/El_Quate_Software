using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Comprobantes;
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
/// Pruebas unitarias (caja blanca) de ComprobantesController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class ComprobantesControllerCrudTests
{
    private readonly Mock<IComprobantesRepository> repoMock = new();
    private readonly ComprobantesController sut;

    public ComprobantesControllerCrudTests()
    {
        sut = new ComprobantesController(repoMock.Object, NullLogger<ComprobantesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetComprobantes ----------

    [Fact]
    public async Task GetComprobantes_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetComprobantes()).ReturnsAsync(new List<Comprobantes> { new Comprobantes { idComprobante = 1, total = 50000m }, new Comprobantes { idComprobante = 2, total = 65000m } });

        var resultado = await sut.GetComprobantes();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetComprobantes_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetComprobantes()).ReturnsAsync(new List<Comprobantes>());

        var resultado = await sut.GetComprobantes();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetComprobantes_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetComprobantes()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetComprobantes();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetComprobanteById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetComprobanteById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetComprobanteById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprobantesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetComprobanteById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetComprobantesById(99)).ReturnsAsync((Comprobantes?)null);

        var resultado = await sut.GetComprobanteById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetComprobanteById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });

        var resultado = await sut.GetComprobanteById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetComprobanteById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetComprobanteById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostComprobante ----------

    [Fact]
    public async Task PostComprobante_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(true);

        var resultado = await sut.PostComprobante(new ComprobanteCrearDTO { idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 50000m });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostComprobantes(It.Is<Comprobantes>(x => x.total == 50000m)), Times.Once);
    }

    [Fact]
    public async Task PostComprobante_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(false);

        var resultado = await sut.PostComprobante(new ComprobanteCrearDTO { idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 50000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostComprobante_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("total", "El campo no es válido.");

        var resultado = await sut.PostComprobante(new ComprobanteCrearDTO { idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 50000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostComprobantes(It.IsAny<Comprobantes>()), Times.Never);
    }

    [Fact]
    public async Task PostComprobante_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostComprobantes(It.IsAny<Comprobantes>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostComprobante(new ComprobanteCrearDTO { idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 50000m });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostComprobante_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostComprobantes(It.IsAny<Comprobantes>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostComprobante(new ComprobanteCrearDTO { idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 50000m });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutComprobante ----------

    [Fact]
    public async Task PutComprobante_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.PutComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(true);

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutComprobantes(It.Is<Comprobantes>(x => x.idComprobante == 1 && x.total == 65000m)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutComprobante_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = id, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprobantesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutComprobante_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetComprobantesById(99)).ReturnsAsync((Comprobantes?)null);

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 99, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutComprobantes(It.IsAny<Comprobantes>()), Times.Never);
    }

    [Fact]
    public async Task PutComprobante_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("total", "El campo no es válido.");

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutComprobantes(It.IsAny<Comprobantes>()), Times.Never);
    }

    [Fact]
    public async Task PutComprobante_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.PutComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(false);

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutComprobante_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.PutComprobantes(It.IsAny<Comprobantes>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutComprobante_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutComprobante(new ComprobanteActualizarDTO { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), idMetodo = 1, total = 65000m });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteComprobante ----------

    [Fact]
    public async Task DeleteComprobante_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.DeleteComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(true);

        var resultado = await sut.DeleteComprobante(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteComprobantes(It.Is<Comprobantes>(x => x.idComprobante == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteComprobante_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteComprobante(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetComprobantesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteComprobante_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetComprobantesById(99)).ReturnsAsync((Comprobantes?)null);

        var resultado = await sut.DeleteComprobante(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteComprobantes(It.IsAny<Comprobantes>()), Times.Never);
    }

    [Fact]
    public async Task DeleteComprobante_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.DeleteComprobantes(It.IsAny<Comprobantes>())).ReturnsAsync(false);

        var resultado = await sut.DeleteComprobante(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteComprobante_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ReturnsAsync(new Comprobantes { idComprobante = 1, total = 50000m });
        repoMock.Setup(r => r.DeleteComprobantes(It.IsAny<Comprobantes>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteComprobante(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteComprobante_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetComprobantesById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteComprobante(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
