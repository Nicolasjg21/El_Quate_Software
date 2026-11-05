using System.Security.Claims;
using Backend.Tests.Ayudas;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Kardex;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// KardexController.PostKardex: reglas del inventario.
/// ENTRADA y SALIDA exigen cantidad &gt; 0; AJUSTE exige permiso de inventario y stock nuevo &gt;= 0;
/// el usuario sale del token (no del cuerpo) y una salida sin stock responde 409.
/// </summary>
public class KardexMovimientosTests
{
    private readonly Mock<IKardexRepository> repoMock = new();
    private readonly Mock<IAuthorizationService> authMock = new();
    private readonly KardexController sut;

    public KardexMovimientosTests()
    {
        PuedeAjustar(false);
        repoMock.Setup(r => r.RegistrarMovimiento(It.IsAny<Kardex>())).ReturnsAsync((true, 10));
        sut = new KardexController(repoMock.Object, authMock.Object, NullLogger<KardexController>.Instance)
        {
            ControllerContext = Respuestas.ContextoConUsuario(7)
        };
    }

    private void PuedeAjustar(bool valor) =>
        authMock.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(valor ? AuthorizationResult.Success() : AuthorizationResult.Failed());

    private static KardexCrearDTO Mov(string tipo, int cantidad = 5, int stockNuevo = 0) => new()
    {
        idProducto = 1, tipoMovimiento = tipo, cantidad = cantidad, stockNuevo = stockNuevo, motivo = "Prueba", fecha = new DateTime(2026, 11, 5)
    };

    [Theory]
    [InlineData("ENTRADA")]
    [InlineData("SALIDA")]
    public async Task PostKardex_EntradaOSalidaValida_Retorna200(string tipo)
    {
        (await sut.PostKardex(Mov(tipo))).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PostKardex_TipoEnMinusculasYConEspacios_SeNormaliza()
    {
        await sut.PostKardex(Mov("  entrada "));

        repoMock.Verify(r => r.RegistrarMovimiento(It.Is<Kardex>(k => k.tipoMovimiento == "ENTRADA")), Times.Once);
    }

    [Fact]
    public async Task PostKardex_UsuarioSeTomaDelTokenYNoDelCuerpo()
    {
        await sut.PostKardex(Mov("ENTRADA"));

        repoMock.Verify(r => r.RegistrarMovimiento(It.Is<Kardex>(k => k.idUsuario == 7)), Times.Once);
    }

    [Theory]
    [InlineData("PRESTAMO")]
    [InlineData("")]
    public async Task PostKardex_TipoInvalido_Retorna400(string tipo)
    {
        (await sut.PostKardex(Mov(tipo))).Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.RegistrarMovimiento(It.IsAny<Kardex>()), Times.Never);
    }

    [Theory]
    [InlineData("ENTRADA", 0)]
    [InlineData("SALIDA", -1)]
    public async Task PostKardex_CantidadNoPositiva_Retorna400(string tipo, int cantidad)
    {
        (await sut.PostKardex(Mov(tipo, cantidad))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostKardex_AjusteSinPermiso_Retorna403()
    {
        (await sut.PostKardex(Mov("AJUSTE", stockNuevo: 20))).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        repoMock.Verify(r => r.RegistrarMovimiento(It.IsAny<Kardex>()), Times.Never);
    }

    [Fact]
    public async Task PostKardex_AjusteConPermisoYStockNegativo_Retorna400()
    {
        PuedeAjustar(true);
        (await sut.PostKardex(Mov("AJUSTE", stockNuevo: -1))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostKardex_AjusteConPermisoAStockCero_Retorna200()
    {
        PuedeAjustar(true);
        // En un ajuste la cantidad no se valida: lo que importa es el stock final (0 es válido).
        (await sut.PostKardex(Mov("AJUSTE", cantidad: 0, stockNuevo: 0))).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PostKardex_SalidaSinStockSuficiente_Retorna409ConElStockDisponible()
    {
        repoMock.Setup(r => r.RegistrarMovimiento(It.IsAny<Kardex>())).ReturnsAsync((false, 3));

        var resultado = await sut.PostKardex(Mov("SALIDA", 5));

        resultado.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(resultado, "message")!.ToString().Should().Contain("3");
    }

    [Fact]
    public async Task PostKardex_SinUsuarioIdentificado_Retorna401()
    {
        sut.ControllerContext = Respuestas.ContextoConUsuario(null);

        (await sut.PostKardex(Mov("ENTRADA"))).Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task PostKardex_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("idProducto", "Obligatorio");
        (await sut.PostKardex(Mov("ENTRADA"))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostKardex_ProductoInexistente_Retorna409()
    {
        repoMock.Setup(r => r.RegistrarMovimiento(It.IsAny<Kardex>())).ThrowsAsync(new DbUpdateException("FK idProducto"));
        (await sut.PostKardex(Mov("ENTRADA"))).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostKardex_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.RegistrarMovimiento(It.IsAny<Kardex>())).ThrowsAsync(new Exception("Error"));
        (await sut.PostKardex(Mov("ENTRADA"))).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
