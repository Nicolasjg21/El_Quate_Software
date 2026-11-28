using Backend.Tests.Ayudas;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Auditorias;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// AuditoriasController.PostAuditoria: el usuario y la fecha los pone el servidor,
/// así nadie puede registrar una auditoría a nombre de otro ni con otra fecha.
/// </summary>
public class AuditoriasRegistroTests
{
    private readonly Mock<IAuditoriasRepository> repoMock = new();
    private readonly AuditoriasController sut;

    public AuditoriasRegistroTests()
    {
        sut = new AuditoriasController(repoMock.Object, NullLogger<AuditoriasController>.Instance)
        {
            ControllerContext = Respuestas.ContextoConUsuario(7)
        };
    }

    private static AuditoriaCrearDTO Dto() => new()
    {
        tabla = "Productos", accion = "UPDATE", fecha = new DateTime(2000, 1, 1), datosAnteriores = "{}", datosNuevos = "{}"
    };

    [Fact]
    public async Task PostAuditoria_Valida_UsaElUsuarioDelTokenYLaFechaDelServidor()
    {
        repoMock.Setup(r => r.PostAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(true);
        var antes = DateTime.Now.AddSeconds(-5);

        var resultado = await sut.PostAuditoria(Dto());

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostAuditorias(It.Is<Auditorias>(a => a.idUsuario == 7 && a.fecha >= antes)), Times.Once);
    }

    [Fact]
    public async Task PostAuditoria_SinUsuarioIdentificado_Retorna401()
    {
        sut.ControllerContext = Respuestas.ContextoConUsuario(null);

        (await sut.PostAuditoria(Dto())).Should().BeOfType<UnauthorizedObjectResult>();
        repoMock.Verify(r => r.PostAuditorias(It.IsAny<Auditorias>()), Times.Never);
    }

    [Fact]
    public async Task PostAuditoria_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("tabla", "Obligatorio");
        (await sut.PostAuditoria(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostAuditoria_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostAuditorias(It.IsAny<Auditorias>())).ReturnsAsync(false);
        (await sut.PostAuditoria(Dto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostAuditoria_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostAuditorias(It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("FK"));
        (await sut.PostAuditoria(Dto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostAuditoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostAuditorias(It.IsAny<Auditorias>())).ThrowsAsync(new Exception("Error"));
        (await sut.PostAuditoria(Dto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
