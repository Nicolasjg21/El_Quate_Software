using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Permisos;
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
/// Pruebas unitarias (caja blanca) de PermisosController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class PermisosControllerCrudTests
{
    private readonly Mock<IPermisosRepository> repoMock = new();
    private readonly PermisosController sut;

    public PermisosControllerCrudTests()
    {
        sut = new PermisosController(repoMock.Object, NullLogger<PermisosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetPermisos ----------

    [Fact]
    public async Task GetPermisos_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetPermisos()).ReturnsAsync(new List<Permisos> { new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" }, new Permisos { idPermiso = 2, nombrePermiso = "ventas.anular" } });

        var resultado = await sut.GetPermisos();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPermisos_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetPermisos()).ReturnsAsync(new List<Permisos>());

        var resultado = await sut.GetPermisos();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPermisos_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetPermisos()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetPermisos();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetPermisosById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPermisosById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetPermisosById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPermisosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPermisosById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetPermisosById(99)).ReturnsAsync((Permisos?)null);

        var resultado = await sut.GetPermisosById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPermisosById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });

        var resultado = await sut.GetPermisosById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPermisosById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetPermisosById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostPermisos ----------

    [Fact]
    public async Task PostPermisos_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostPermisos(It.IsAny<Permisos>())).ReturnsAsync(true);

        var resultado = await sut.PostPermisos(new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostPermisos(It.Is<Permisos>(x => x.nombrePermiso == "ventas.cobrar")), Times.Once);
    }

    [Fact]
    public async Task PostPermisos_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostPermisos(It.IsAny<Permisos>())).ReturnsAsync(false);

        var resultado = await sut.PostPermisos(new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostPermisos_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombrePermiso", "El campo es obligatorio.");

        var resultado = await sut.PostPermisos(new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostPermisos(It.IsAny<Permisos>()), Times.Never);
    }

    [Fact]
    public async Task PostPermisos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostPermisos(It.IsAny<Permisos>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostPermisos(new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostPermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostPermisos(It.IsAny<Permisos>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostPermisos(new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutPermisos ----------

    [Fact]
    public async Task PutPermisos_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.PutPermisos(It.IsAny<Permisos>())).ReturnsAsync(true);

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 1, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutPermisos(It.Is<Permisos>(x => x.idPermiso == 1 && x.nombrePermiso == "ventas.anular")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutPermisos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = id, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPermisosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutPermisos_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetPermisosById(99)).ReturnsAsync((Permisos?)null);

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 99, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutPermisos(It.IsAny<Permisos>()), Times.Never);
    }

    [Fact]
    public async Task PutPermisos_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombrePermiso", "El campo es obligatorio.");

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 1, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutPermisos(It.IsAny<Permisos>()), Times.Never);
    }

    [Fact]
    public async Task PutPermisos_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.PutPermisos(It.IsAny<Permisos>())).ReturnsAsync(false);

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 1, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutPermisos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.PutPermisos(It.IsAny<Permisos>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 1, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutPermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutPermisos(new PermisoActualizarDTO { idPermiso = 1, nombrePermiso = "ventas.anular" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeletePermisos ----------

    [Fact]
    public async Task DeletePermisos_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.DeletePermisos(It.IsAny<Permisos>())).ReturnsAsync(true);

        var resultado = await sut.DeletePermisos(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeletePermisos(It.Is<Permisos>(x => x.idPermiso == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeletePermisos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeletePermisos(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPermisosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeletePermisos_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetPermisosById(99)).ReturnsAsync((Permisos?)null);

        var resultado = await sut.DeletePermisos(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeletePermisos(It.IsAny<Permisos>()), Times.Never);
    }

    [Fact]
    public async Task DeletePermisos_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.DeletePermisos(It.IsAny<Permisos>())).ReturnsAsync(false);

        var resultado = await sut.DeletePermisos(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeletePermisos_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ReturnsAsync(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        repoMock.Setup(r => r.DeletePermisos(It.IsAny<Permisos>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeletePermisos(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeletePermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetPermisosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeletePermisos(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
