using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Pruebas unitarias (caja blanca) de MesasController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class MesasControllerCrudTests
{
    private readonly Mock<IMesasRepository> repoMock = new();
    private readonly Mock<IAuthorizationService> authMock = new();
    private readonly MesasController sut;

    public MesasControllerCrudTests()
    {
        PuedeAdministrarMesas(true);
        sut = new MesasController(repoMock.Object, authMock.Object, NullLogger<MesasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private void PuedeAdministrarMesas(bool valor) =>
        authMock.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(valor ? AuthorizationResult.Success() : AuthorizationResult.Failed());

    // ---------- GetMesas ----------

    [Fact]
    public async Task GetMesas_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetMesas()).ReturnsAsync(new List<Mesas> { new Mesas { idMesa = 1, estado = "Libre" }, new Mesas { idMesa = 2, estado = "Ocupada" } });

        var resultado = await sut.GetMesas();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMesas_ListaVacia_Retorna200ConListaVacia()
    {
        repoMock.Setup(r => r.GetMesas()).ReturnsAsync(new List<Mesas>());

        var resultado = await sut.GetMesas();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMesas_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetMesas()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetMesas();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetMesaById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMesaById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetMesaById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMesasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMesaById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetMesasById(99)).ReturnsAsync((Mesas?)null);

        var resultado = await sut.GetMesaById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMesaById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });

        var resultado = await sut.GetMesaById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMesaById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetMesaById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostMesa ----------

    [Fact]
    public async Task PostMesa_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostMesas(It.IsAny<Mesas>())).ReturnsAsync(true);

        var resultado = await sut.PostMesa(new MesaCrearDTO { numeroMesa = 3, estado = "Libre" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostMesas(It.Is<Mesas>(x => x.estado == "Libre")), Times.Once);
    }

    [Fact]
    public async Task PostMesa_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostMesas(It.IsAny<Mesas>())).ReturnsAsync(false);

        var resultado = await sut.PostMesa(new MesaCrearDTO { numeroMesa = 3, estado = "Libre" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostMesa_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("estado", "El campo no es válido.");

        var resultado = await sut.PostMesa(new MesaCrearDTO { numeroMesa = 3, estado = "Libre" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostMesas(It.IsAny<Mesas>()), Times.Never);
    }

    [Fact]
    public async Task PostMesa_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostMesas(It.IsAny<Mesas>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostMesa(new MesaCrearDTO { numeroMesa = 3, estado = "Libre" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostMesa_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostMesas(It.IsAny<Mesas>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostMesa(new MesaCrearDTO { numeroMesa = 3, estado = "Libre" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutMesa: qué puede hacer cada rol ----------

    [Fact]
    public async Task PutMesa_SinPermiso_OcuparMesaLibre_Retorna200()
    {
        PuedeAdministrarMesas(false);
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        repoMock.Setup(r => r.PutMesas(It.IsAny<Mesas>())).ReturnsAsync(true);

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(3, "Libre", 3, "Inactiva")]    // retirar
    [InlineData(3, "Inactiva", 3, "Libre")]    // reactivar
    [InlineData(3, "Libre", 9, "Libre")]       // renumerar
    public async Task PutMesa_SinPermiso_AdministrarMesa_Retorna403SinActualizar(int numeroActual, string estadoActual, int numeroNuevo, string estadoNuevo)
    {
        PuedeAdministrarMesas(false);
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, numeroMesa = numeroActual, estado = estadoActual });

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = numeroNuevo, estado = estadoNuevo });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        repoMock.Verify(r => r.PutMesas(It.IsAny<Mesas>()), Times.Never);
    }

    [Fact]
    public async Task PutMesa_ConPermiso_RetirarMesa_Retorna200()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        repoMock.Setup(r => r.PutMesas(It.IsAny<Mesas>())).ReturnsAsync(true);

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Inactiva" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutMesas(It.Is<Mesas>(x => x.estado == "Inactiva")), Times.Once);
    }

    // ---------- PutMesa ----------

    [Fact]
    public async Task PutMesa_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.PutMesas(It.IsAny<Mesas>())).ReturnsAsync(true);

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutMesas(It.Is<Mesas>(x => x.idMesa == 1 && x.estado == "Ocupada")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutMesa_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = id, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMesasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutMesa_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetMesasById(99)).ReturnsAsync((Mesas?)null);

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 99, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutMesas(It.IsAny<Mesas>()), Times.Never);
    }

    [Fact]
    public async Task PutMesa_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("estado", "El campo no es válido.");

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutMesas(It.IsAny<Mesas>()), Times.Never);
    }

    [Fact]
    public async Task PutMesa_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.PutMesas(It.IsAny<Mesas>())).ReturnsAsync(false);

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutMesa_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.PutMesas(It.IsAny<Mesas>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutMesa_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutMesa(new MesaActualizarDTO { idMesa = 1, numeroMesa = 3, estado = "Ocupada" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteMesa ----------

    [Fact]
    public async Task DeleteMesa_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.DeleteMesas(It.IsAny<Mesas>())).ReturnsAsync(true);

        var resultado = await sut.DeleteMesa(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteMesas(It.Is<Mesas>(x => x.idMesa == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteMesa_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteMesa(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetMesasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMesa_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetMesasById(99)).ReturnsAsync((Mesas?)null);

        var resultado = await sut.DeleteMesa(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteMesas(It.IsAny<Mesas>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMesa_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.DeleteMesas(It.IsAny<Mesas>())).ReturnsAsync(false);

        var resultado = await sut.DeleteMesa(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteMesa_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ReturnsAsync(new Mesas { idMesa = 1, estado = "Libre" });
        repoMock.Setup(r => r.DeleteMesas(It.IsAny<Mesas>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteMesa(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteMesa_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetMesasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteMesa(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
