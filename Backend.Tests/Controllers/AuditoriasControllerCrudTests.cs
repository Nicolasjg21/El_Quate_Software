using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Auditorias;
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
/// Pruebas unitarias (caja blanca) de AuditoriasController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class AuditoriasControllerCrudTests
{
    private readonly Mock<IAuditoriasRepository> repoMock = new();
    private readonly AuditoriasController sut;

    public AuditoriasControllerCrudTests()
    {
        sut = new AuditoriasController(repoMock.Object, NullLogger<AuditoriasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetAuditorias ----------

    [Fact]
    public async Task GetAuditorias_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetAuditorias()).ReturnsAsync(new List<Auditorias> { new Auditorias { idAuditoria = 1, accion = "INSERT" }, new Auditorias { idAuditoria = 2, accion = "UPDATE" } });

        var resultado = await sut.GetAuditorias();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditorias_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetAuditorias()).ReturnsAsync(new List<Auditorias>());

        var resultado = await sut.GetAuditorias();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAuditorias_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetAuditorias()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetAuditorias();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetAuditoriaById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAuditoriaById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetAuditoriaById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetAuditoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAuditoriaById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetAuditoriasById(99)).ReturnsAsync((Auditorias?)null);

        var resultado = await sut.GetAuditoriaById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAuditoriaById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });

        var resultado = await sut.GetAuditoriaById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditoriaById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetAuditoriaById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutAuditoria ----------

    [Fact]
    public async Task PutAuditoria_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.PutAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(true);

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 1, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutAuditorias(It.Is<Auditorias>(x => x.idAuditoria == 1 && x.accion == "UPDATE")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutAuditoria_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = id, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetAuditoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutAuditoria_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetAuditoriasById(99)).ReturnsAsync((Auditorias?)null);

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 99, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutAuditorias(It.IsAny<Auditorias>()), Times.Never);
    }

    [Fact]
    public async Task PutAuditoria_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("accion", "El campo no es válido.");

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 1, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutAuditorias(It.IsAny<Auditorias>()), Times.Never);
    }

    [Fact]
    public async Task PutAuditoria_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.PutAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(false);

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 1, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutAuditoria_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.PutAuditorias(It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 1, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutAuditoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutAuditoria(new AuditoriaActualizarDTO { idAuditoria = 1, idUsuario = 1, tabla = "Productos", fecha = new DateTime(2026, 11, 5), accion = "UPDATE" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteAuditoria ----------

    [Fact]
    public async Task DeleteAuditoria_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.DeleteAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(true);

        var resultado = await sut.DeleteAuditoria(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteAuditorias(It.Is<Auditorias>(x => x.idAuditoria == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteAuditoria_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteAuditoria(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetAuditoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAuditoria_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetAuditoriasById(99)).ReturnsAsync((Auditorias?)null);

        var resultado = await sut.DeleteAuditoria(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteAuditorias(It.IsAny<Auditorias>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAuditoria_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.DeleteAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(false);

        var resultado = await sut.DeleteAuditoria(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteAuditoria_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ReturnsAsync(new Auditorias { idAuditoria = 1, accion = "INSERT" });
        repoMock.Setup(r => r.DeleteAuditorias(It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteAuditoria(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteAuditoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetAuditoriasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteAuditoria(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
