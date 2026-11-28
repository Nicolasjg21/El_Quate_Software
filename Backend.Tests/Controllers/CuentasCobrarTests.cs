using Backend.Tests.Ayudas;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// CuentasController.Cobrar: cada resultado del repositorio se traduce a su código HTTP, y el
/// usuario que cobra sale siempre del token (nunca del cuerpo).
/// </summary>
public class CuentasCobrarTests
{
    private readonly Mock<ICuentasRepository> repoMock = new();

    private CuentasController Sut(int? idUsuario = 2) =>
        new(repoMock.Object, NullLogger<CuentasController>.Instance) { ControllerContext = Respuestas.ContextoConUsuario(idUsuario) };

    private static readonly CobrarCuentaDTO solicitud = new() { idCuenta = 1, idMetodo = 1, totalEsperado = 5000m };

    private static ResultadoCobro Resultado(EstadoCobro estado, decimal total = 5000m)
    {
        var cuenta = new Cuentas
        {
            idCuenta = 1, idMesa = 1, estado = "Cerrada", total = total,
            fechaApertura = new DateTime(2026, 11, 28, 20, 0, 0), fechaCierre = new DateTime(2026, 11, 28, 21, 0, 0),
            mesa = new Mesas { idMesa = 1, numeroMesa = 2, estado = "Libre" }
        };
        var comprobante = new Comprobantes
        {
            idComprobante = 7, idCuenta = 1, idMetodo = 1, total = total, baseGravable = 4630m, impuesto = 370m,
            tasaImpuesto = 0.08m, fecha = new DateTime(2026, 11, 28, 21, 0, 0)
        };
        return new ResultadoCobro(estado, cuenta, comprobante, total);
    }

    private void Devuelve(ResultadoCobro resultado) =>
        repoMock.Setup(r => r.Cobrar(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int>())).ReturnsAsync(resultado);

    [Fact]
    public async Task Cobrar_Cobrada_Retorna200ConElComprobante()
    {
        Devuelve(Resultado(EstadoCobro.Cobrada));

        var r = await Sut().Cobrar(solicitud);

        r.Should().BeOfType<OkObjectResult>();
        var data = Respuestas.Leer(r, "data").Should().BeOfType<CobroRespuestaDTO>().Subject;
        data.idComprobante.Should().Be(7);
        data.numeroMesa.Should().Be(2);
        data.total.Should().Be(5000m);
        data.baseGravable.Should().Be(4630m);
        data.impuesto.Should().Be(370m);
        data.fechaApertura.Should().Be(new DateTime(2026, 11, 28, 20, 0, 0));
        data.fechaCierre.Should().Be(new DateTime(2026, 11, 28, 21, 0, 0));
    }

    [Fact]
    public async Task Cobrar_UsaElUsuarioDelTokenYLosDatosDeLaSolicitud()
    {
        Devuelve(Resultado(EstadoCobro.Cobrada));

        await Sut(idUsuario: 15).Cobrar(solicitud);

        repoMock.Verify(r => r.Cobrar(1, 1, 5000m, 15), Times.Once);
    }

    [Fact]
    public async Task Cobrar_SinUsuarioEnElToken_Retorna401SinCobrar()
    {
        var r = await Sut(idUsuario: null).Cobrar(solicitud);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        repoMock.Verify(x => x.Cobrar(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Cobrar_ModeloInvalido_Retorna400SinCobrar()
    {
        var sut = Sut();
        sut.ModelState.AddModelError("idCuenta", "El campo idCuenta debe ser mayor que cero.");

        var r = await sut.Cobrar(new CobrarCuentaDTO());

        r.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(x => x.Cobrar(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Cobrar_YaCobrada_Retorna409ConCodigoYElComprobanteExistente()
    {
        Devuelve(Resultado(EstadoCobro.YaCobrada));

        var r = await Sut().Cobrar(solicitud);

        r.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(r, "codigo").Should().Be("CUENTA_YA_COBRADA");
        Respuestas.Leer(r, "data").Should().BeOfType<CobroRespuestaDTO>().Which.idComprobante.Should().Be(7);
    }

    [Fact]
    public async Task Cobrar_TotalDesactualizado_Retorna409ConElTotalActual()
    {
        Devuelve(new ResultadoCobro(EstadoCobro.TotalDesactualizado, totalActual: 10000m));

        var r = await Sut().Cobrar(solicitud);

        r.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(r, "codigo").Should().Be("TOTAL_DESACTUALIZADO");
        Respuestas.Leer(r, "totalActual").Should().Be(10000m);
    }

    [Fact]
    public async Task Cobrar_CuentaNoAbierta_Retorna409()
    {
        Devuelve(new ResultadoCobro(EstadoCobro.CuentaNoAbierta));

        var r = await Sut().Cobrar(solicitud);

        r.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(r, "codigo").Should().Be("CUENTA_NO_ABIERTA");
    }

    [Fact]
    public async Task Cobrar_CuentaNoExiste_Retorna404()
    {
        Devuelve(new ResultadoCobro(EstadoCobro.CuentaNoExiste));

        (await Sut().Cobrar(solicitud)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(EstadoCobro.MetodoNoExiste)]
    [InlineData(EstadoCobro.SinProductos)]
    public async Task Cobrar_DatosQueNoPermitenCobrar_Retorna400(EstadoCobro estado)
    {
        Devuelve(new ResultadoCobro(estado));

        (await Sut().Cobrar(solicitud)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cobrar_IndiceUnicoRechazaElComprobante_Retorna409()
    {
        repoMock.Setup(r => r.Cobrar(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int>()))
            .ThrowsAsync(new DbUpdateException("UX_Comprobantes_idCuenta"));

        var r = await Sut().Cobrar(solicitud);

        r.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(r, "codigo").Should().Be("CONFLICTO_COBRO");
    }

    [Fact]
    public async Task Cobrar_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.Cobrar(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal?>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Base de datos caída"));

        (await Sut().Cobrar(solicitud)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
