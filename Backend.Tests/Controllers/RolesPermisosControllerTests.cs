using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.RolesPermisos;
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
/// RolesPermisosController: la llave es compuesta (idRol + idPermiso), así que cada camino
/// se prueba con las dos partes. Asignar un permiso que el rol ya tiene se rechaza con 400.
/// </summary>
public class RolesPermisosControllerTests
{
    private readonly Mock<IRolesPermisosRepository> repoMock = new();
    private readonly RolesPermisosController sut;

    public RolesPermisosControllerTests()
    {
        sut = new RolesPermisosController(repoMock.Object, NullLogger<RolesPermisosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static RolesPermisos Asignacion() => new() { idRol = 2, idPermiso = 5 };
    private static RolPermisoCrearDTO Dto(int idRol = 2, int idPermiso = 5) => new() { idRol = idRol, idPermiso = idPermiso };

    // ---------- GetRolesPermisos ----------

    [Fact]
    public async Task GetRolesPermisos_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetRolesPermisos()).ReturnsAsync(new List<RolesPermisos> { Asignacion() });

        (await sut.GetRolesPermisos()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRolesPermisos_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetRolesPermisos()).ReturnsAsync(new List<RolesPermisos>());

        (await sut.GetRolesPermisos()).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRolesPermisos_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesPermisos()).ThrowsAsync(new Exception("Base de datos caída"));

        (await sut.GetRolesPermisos()).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetRolesPermisosById ----------

    [Theory]
    [InlineData(0, 5)]
    [InlineData(2, 0)]
    [InlineData(-1, -1)]
    public async Task GetRolesPermisosById_AlgunIdNoPositivo_Retorna400(int idRol, int idPermiso)
    {
        (await sut.GetRolesPermisosById(idRol, idPermiso)).Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetRolesPermisosById(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetRolesPermisosById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 99)).ReturnsAsync((RolesPermisos?)null);

        (await sut.GetRolesPermisosById(2, 99)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRolesPermisosById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());

        (await sut.GetRolesPermisosById(2, 5)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRolesPermisosById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ThrowsAsync(new Exception("Base de datos caída"));

        (await sut.GetRolesPermisosById(2, 5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostRolesPermisos ----------

    [Fact]
    public async Task PostRolesPermisos_AsignacionNueva_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync((RolesPermisos?)null);
        repoMock.Setup(r => r.PostRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(true);

        var resultado = await sut.PostRolesPermisos(Dto());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostRolesPermisos(It.Is<RolesPermisos>(x => x.idRol == 2 && x.idPermiso == 5)), Times.Once);
    }

    [Fact]
    public async Task PostRolesPermisos_ElRolYaTieneElPermiso_Retorna400SinGuardar()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());

        var resultado = await sut.PostRolesPermisos(Dto());

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostRolesPermisos(It.IsAny<RolesPermisos>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(2, 0)]
    public async Task PostRolesPermisos_AlgunIdNoPositivo_Retorna400(int idRol, int idPermiso)
    {
        (await sut.PostRolesPermisos(Dto(idRol, idPermiso))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostRolesPermisos_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("idRol", "Obligatorio");

        (await sut.PostRolesPermisos(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostRolesPermisos_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync((RolesPermisos?)null);
        repoMock.Setup(r => r.PostRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(false);

        (await sut.PostRolesPermisos(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostRolesPermisos_RolOPermisoInexistente_Retorna409()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync((RolesPermisos?)null);
        repoMock.Setup(r => r.PostRolesPermisos(It.IsAny<RolesPermisos>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.PostRolesPermisos(Dto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostRolesPermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ThrowsAsync(new Exception("Error inesperado"));

        (await sut.PostRolesPermisos(Dto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutRolesPermisos ----------

    [Fact]
    public async Task PutRolesPermisos_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.PutRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(true);

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PutRolesPermisos_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync((RolesPermisos?)null);

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutRolesPermisos(It.IsAny<RolesPermisos>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(2, 0)]
    public async Task PutRolesPermisos_AlgunIdNoPositivo_Retorna400(int idRol, int idPermiso)
    {
        (await sut.PutRolesPermisos(Dto(idRol, idPermiso))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutRolesPermisos_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("idRol", "Obligatorio");

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutRolesPermisos_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.PutRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(false);

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutRolesPermisos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.PutRolesPermisos(It.IsAny<RolesPermisos>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutRolesPermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ThrowsAsync(new Exception("Error inesperado"));

        (await sut.PutRolesPermisos(Dto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteRolesPermisos ----------

    [Fact]
    public async Task DeleteRolesPermisos_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.DeleteRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(true);

        (await sut.DeleteRolesPermisos(2, 5)).Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteRolesPermisos(It.Is<RolesPermisos>(x => x.idRol == 2 && x.idPermiso == 5)), Times.Once);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(2, 0)]
    public async Task DeleteRolesPermisos_AlgunIdNoPositivo_Retorna400(int idRol, int idPermiso)
    {
        (await sut.DeleteRolesPermisos(idRol, idPermiso)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteRolesPermisos_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync((RolesPermisos?)null);

        (await sut.DeleteRolesPermisos(2, 5)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteRolesPermisos_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.DeleteRolesPermisos(It.IsAny<RolesPermisos>())).ReturnsAsync(false);

        (await sut.DeleteRolesPermisos(2, 5)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteRolesPermisos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ReturnsAsync(Asignacion());
        repoMock.Setup(r => r.DeleteRolesPermisos(It.IsAny<RolesPermisos>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.DeleteRolesPermisos(2, 5)).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteRolesPermisos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesPermisosById(2, 5)).ThrowsAsync(new Exception("Error inesperado"));

        (await sut.DeleteRolesPermisos(2, 5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
