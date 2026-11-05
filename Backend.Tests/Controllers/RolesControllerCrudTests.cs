using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Roles;
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
/// Pruebas unitarias (caja blanca) de RolesController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class RolesControllerCrudTests
{
    private readonly Mock<IRolesRepository> repoMock = new();
    private readonly RolesController sut;

    public RolesControllerCrudTests()
    {
        sut = new RolesController(repoMock.Object, NullLogger<RolesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetRoles ----------

    [Fact]
    public async Task GetRoles_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetRoles()).ReturnsAsync(new List<Roles> { new Roles { idRol = 1, nombreRol = "Mesero" }, new Roles { idRol = 2, nombreRol = "Cajero" } });

        var resultado = await sut.GetRoles();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoles_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetRoles()).ReturnsAsync(new List<Roles>());

        var resultado = await sut.GetRoles();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRoles_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetRoles()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetRoles();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetRolesById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRolesById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetRolesById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetRolesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetRolesById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetRolesById(99)).ReturnsAsync((Roles?)null);

        var resultado = await sut.GetRolesById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRolesById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });

        var resultado = await sut.GetRolesById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRolesById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetRolesById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostRoles ----------

    [Fact]
    public async Task PostRoles_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostRoles(It.IsAny<Roles>())).ReturnsAsync(true);

        var resultado = await sut.PostRoles(new RolCrearDTO { nombreRol = "Mesero" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostRoles(It.Is<Roles>(x => x.nombreRol == "Mesero")), Times.Once);
    }

    [Fact]
    public async Task PostRoles_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostRoles(It.IsAny<Roles>())).ReturnsAsync(false);

        var resultado = await sut.PostRoles(new RolCrearDTO { nombreRol = "Mesero" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostRoles_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombreRol", "El campo es obligatorio.");

        var resultado = await sut.PostRoles(new RolCrearDTO { nombreRol = "Mesero" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostRoles(It.IsAny<Roles>()), Times.Never);
    }

    [Fact]
    public async Task PostRoles_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostRoles(It.IsAny<Roles>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostRoles(new RolCrearDTO { nombreRol = "Mesero" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostRoles_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostRoles(It.IsAny<Roles>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostRoles(new RolCrearDTO { nombreRol = "Mesero" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutRoles ----------

    [Fact]
    public async Task PutRoles_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.PutRoles(It.IsAny<Roles>())).ReturnsAsync(true);

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 1, nombreRol = "Cajero" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutRoles(It.Is<Roles>(x => x.idRol == 1 && x.nombreRol == "Cajero")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutRoles_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = id, nombreRol = "Cajero" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetRolesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutRoles_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetRolesById(99)).ReturnsAsync((Roles?)null);

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 99, nombreRol = "Cajero" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutRoles(It.IsAny<Roles>()), Times.Never);
    }

    [Fact]
    public async Task PutRoles_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreRol", "El campo es obligatorio.");

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 1, nombreRol = "Cajero" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutRoles(It.IsAny<Roles>()), Times.Never);
    }

    [Fact]
    public async Task PutRoles_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.PutRoles(It.IsAny<Roles>())).ReturnsAsync(false);

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 1, nombreRol = "Cajero" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutRoles_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.PutRoles(It.IsAny<Roles>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 1, nombreRol = "Cajero" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutRoles_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutRoles(new RolActualizarDTO { idRol = 1, nombreRol = "Cajero" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteRoles ----------

    [Fact]
    public async Task DeleteRoles_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.DeleteRoles(It.IsAny<Roles>())).ReturnsAsync(true);

        var resultado = await sut.DeleteRoles(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteRoles(It.Is<Roles>(x => x.idRol == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteRoles_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteRoles(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetRolesById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoles_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetRolesById(99)).ReturnsAsync((Roles?)null);

        var resultado = await sut.DeleteRoles(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteRoles(It.IsAny<Roles>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoles_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.DeleteRoles(It.IsAny<Roles>())).ReturnsAsync(false);

        var resultado = await sut.DeleteRoles(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteRoles_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ReturnsAsync(new Roles { idRol = 1, nombreRol = "Mesero" });
        repoMock.Setup(r => r.DeleteRoles(It.IsAny<Roles>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteRoles(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteRoles_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetRolesById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteRoles(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
