using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.TipoDocumento;
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
/// Pruebas unitarias (caja blanca) de TipoDocumentoController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class TipoDocumentoControllerCrudTests
{
    private readonly Mock<ITipoDocumentoRepository> repoMock = new();
    private readonly TipoDocumentoController sut;

    public TipoDocumentoControllerCrudTests()
    {
        sut = new TipoDocumentoController(repoMock.Object, NullLogger<TipoDocumentoController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetTipoDocumento ----------

    [Fact]
    public async Task GetTipoDocumento_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetTipoDocumento()).ReturnsAsync(new List<TipoDocumento> { new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" }, new TipoDocumento { idTipoDocumento = 2, nombreTipo = "Cédula de extranjería" } });

        var resultado = await sut.GetTipoDocumento();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTipoDocumento_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetTipoDocumento()).ReturnsAsync(new List<TipoDocumento>());

        var resultado = await sut.GetTipoDocumento();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTipoDocumento_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetTipoDocumento()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetTipoDocumento();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetTipoDocumentoById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTipoDocumentoById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetTipoDocumentoById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetTipoDocumentoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetTipoDocumentoById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(99)).ReturnsAsync((TipoDocumento?)null);

        var resultado = await sut.GetTipoDocumentoById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTipoDocumentoById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });

        var resultado = await sut.GetTipoDocumentoById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTipoDocumentoById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetTipoDocumentoById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostTipoDocumento ----------

    [Fact]
    public async Task PostTipoDocumento_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(true);

        var resultado = await sut.PostTipoDocumento(new TipoDocumentoCrearDTO { nombreTipo = "Cédula de ciudadanía" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostTipoDocumento(It.Is<TipoDocumento>(x => x.nombreTipo == "Cédula de ciudadanía")), Times.Once);
    }

    [Fact]
    public async Task PostTipoDocumento_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(false);

        var resultado = await sut.PostTipoDocumento(new TipoDocumentoCrearDTO { nombreTipo = "Cédula de ciudadanía" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostTipoDocumento_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombreTipo", "El campo es obligatorio.");

        var resultado = await sut.PostTipoDocumento(new TipoDocumentoCrearDTO { nombreTipo = "Cédula de ciudadanía" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostTipoDocumento(It.IsAny<TipoDocumento>()), Times.Never);
    }

    [Fact]
    public async Task PostTipoDocumento_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostTipoDocumento(It.IsAny<TipoDocumento>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostTipoDocumento(new TipoDocumentoCrearDTO { nombreTipo = "Cédula de ciudadanía" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostTipoDocumento_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostTipoDocumento(It.IsAny<TipoDocumento>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostTipoDocumento(new TipoDocumentoCrearDTO { nombreTipo = "Cédula de ciudadanía" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutTipoDocumento ----------

    [Fact]
    public async Task PutTipoDocumento_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.PutTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(true);

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 1, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutTipoDocumento(It.Is<TipoDocumento>(x => x.idTipoDocumento == 1 && x.nombreTipo == "Cédula de extranjería")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutTipoDocumento_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = id, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetTipoDocumentoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutTipoDocumento_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(99)).ReturnsAsync((TipoDocumento?)null);

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 99, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutTipoDocumento(It.IsAny<TipoDocumento>()), Times.Never);
    }

    [Fact]
    public async Task PutTipoDocumento_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreTipo", "El campo es obligatorio.");

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 1, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutTipoDocumento(It.IsAny<TipoDocumento>()), Times.Never);
    }

    [Fact]
    public async Task PutTipoDocumento_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.PutTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(false);

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 1, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutTipoDocumento_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.PutTipoDocumento(It.IsAny<TipoDocumento>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 1, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutTipoDocumento_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutTipoDocumento(new TipoDocumentoActualizarDTO { idTipoDocumento = 1, nombreTipo = "Cédula de extranjería" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteTipoDocumento ----------

    [Fact]
    public async Task DeleteTipoDocumento_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.DeleteTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(true);

        var resultado = await sut.DeleteTipoDocumento(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteTipoDocumento(It.Is<TipoDocumento>(x => x.idTipoDocumento == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteTipoDocumento_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteTipoDocumento(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetTipoDocumentoById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTipoDocumento_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(99)).ReturnsAsync((TipoDocumento?)null);

        var resultado = await sut.DeleteTipoDocumento(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteTipoDocumento(It.IsAny<TipoDocumento>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTipoDocumento_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.DeleteTipoDocumento(It.IsAny<TipoDocumento>())).ReturnsAsync(false);

        var resultado = await sut.DeleteTipoDocumento(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteTipoDocumento_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ReturnsAsync(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula de ciudadanía" });
        repoMock.Setup(r => r.DeleteTipoDocumento(It.IsAny<TipoDocumento>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteTipoDocumento(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteTipoDocumento_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetTipoDocumentoById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteTipoDocumento(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
