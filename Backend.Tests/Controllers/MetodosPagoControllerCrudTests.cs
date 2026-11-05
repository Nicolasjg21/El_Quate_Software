using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.MetodosPago;
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
/// Pruebas unitarias (caja blanca) de MetodosPagoController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class MetodosPagoControllerCrudTests
{
    private readonly Mock<IMetodosPagoRepository> repoMock = new();
    private readonly MetodosPagoController sut;

    public MetodosPagoControllerCrudTests()
    {
        sut = new MetodosPagoController(repoMock.Object, NullLogger<MetodosPagoController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetMetodosPago ----------

    [Fact]
    public async Task GetMetodosPago_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetMetodosPago()).ReturnsAsync(new List<MetodosPago> { new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" }, new MetodosPago { idMetodo = 2, nombreMetodo = "Nequi" } });

        var resultado = await sut.GetMetodosPago();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMetodosPago_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetMetodosPago()).ReturnsAsync(new List<MetodosPago>());

        var resultado = await sut.GetMetodosPago();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMetodosPago_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetMetodosPago()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetMetodosPago();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetMetodosPagoById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMetodosPagoById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetMetodosPagoById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMetodosPagoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMetodosPagoById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(99)).ReturnsAsync((MetodosPago?)null);

        var resultado = await sut.GetMetodosPagoById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMetodosPagoById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });

        var resultado = await sut.GetMetodosPagoById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMetodosPagoById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetMetodosPagoById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostMetodosPago ----------

    [Fact]
    public async Task PostMetodosPago_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(true);

        var resultado = await sut.PostMetodosPago(new MetodoPagoCrearDTO { nombreMetodo = "Efectivo" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostMetodosPago(It.Is<MetodosPago>(x => x.nombreMetodo == "Efectivo")), Times.Once);
    }

    [Fact]
    public async Task PostMetodosPago_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(false);

        var resultado = await sut.PostMetodosPago(new MetodoPagoCrearDTO { nombreMetodo = "Efectivo" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostMetodosPago_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombreMetodo", "El campo es obligatorio.");

        var resultado = await sut.PostMetodosPago(new MetodoPagoCrearDTO { nombreMetodo = "Efectivo" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostMetodosPago(It.IsAny<MetodosPago>()), Times.Never);
    }

    [Fact]
    public async Task PostMetodosPago_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostMetodosPago(It.IsAny<MetodosPago>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostMetodosPago(new MetodoPagoCrearDTO { nombreMetodo = "Efectivo" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostMetodosPago_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostMetodosPago(It.IsAny<MetodosPago>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostMetodosPago(new MetodoPagoCrearDTO { nombreMetodo = "Efectivo" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutMetodosPago ----------

    [Fact]
    public async Task PutMetodosPago_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.PutMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(true);

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 1, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutMetodosPago(It.Is<MetodosPago>(x => x.idMetodo == 1 && x.nombreMetodo == "Nequi")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutMetodosPago_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = id, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMetodosPagoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutMetodosPago_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(99)).ReturnsAsync((MetodosPago?)null);

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 99, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutMetodosPago(It.IsAny<MetodosPago>()), Times.Never);
    }

    [Fact]
    public async Task PutMetodosPago_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreMetodo", "El campo es obligatorio.");

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 1, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutMetodosPago(It.IsAny<MetodosPago>()), Times.Never);
    }

    [Fact]
    public async Task PutMetodosPago_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.PutMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(false);

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 1, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutMetodosPago_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.PutMetodosPago(It.IsAny<MetodosPago>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 1, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutMetodosPago_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutMetodosPago(new MetodoPagoActualizarDTO { idMetodo = 1, nombreMetodo = "Nequi" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteMetodosPago ----------

    [Fact]
    public async Task DeleteMetodosPago_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.DeleteMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(true);

        var resultado = await sut.DeleteMetodosPago(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteMetodosPago(It.Is<MetodosPago>(x => x.idMetodo == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteMetodosPago_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteMetodosPago(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMetodosPagoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMetodosPago_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(99)).ReturnsAsync((MetodosPago?)null);

        var resultado = await sut.DeleteMetodosPago(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteMetodosPago(It.IsAny<MetodosPago>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMetodosPago_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.DeleteMetodosPago(It.IsAny<MetodosPago>())).ReturnsAsync(false);

        var resultado = await sut.DeleteMetodosPago(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteMetodosPago_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ReturnsAsync(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        repoMock.Setup(r => r.DeleteMetodosPago(It.IsAny<MetodosPago>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteMetodosPago(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteMetodosPago_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetMetodosPagoById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteMetodosPago(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
