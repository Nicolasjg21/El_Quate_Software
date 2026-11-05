using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>ManejadorExcepcionesGlobal: la última red de seguridad responde 500 genérico y no filtra detalles internos.</summary>
public class ManejadorExcepcionesGlobalTests
{
    private readonly ManejadorExcepcionesGlobal sut = new(NullLogger<ManejadorExcepcionesGlobal>.Instance);

    private static DefaultHttpContext Contexto()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static string Cuerpo(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        return new StreamReader(ctx.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task TryHandleAsync_ExcepcionNoControlada_Responde500ConMensajeGenerico()
    {
        var ctx = Contexto();

        var manejada = await sut.TryHandleAsync(ctx, new InvalidOperationException("detalle interno"), CancellationToken.None);

        manejada.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(500);
        // System.Text.Json escapa las tildes ("ó" → \u00F3), por eso se busca la parte sin tilde.
        Cuerpo(ctx).Should().Contain("error interno en el servidor").And.Contain("\"statusCode\":500");
    }

    [Fact]
    public async Task TryHandleAsync_NoFiltraElMensajeNiLaPilaDeLaExcepcion()
    {
        var ctx = Contexto();

        await sut.TryHandleAsync(ctx, new Exception("Server=tcp:elquate.database.windows.net;Password=secreta"), CancellationToken.None);

        var cuerpo = Cuerpo(ctx);
        cuerpo.Should().NotContain("Password").And.NotContain("database.windows.net").And.NotContain("StackTrace");
    }

    [Fact]
    public async Task TryHandleAsync_ClienteCanceloLaPeticion_NoEscribeRespuesta()
    {
        var ctx = Contexto();
        ctx.RequestAborted = new CancellationToken(canceled: true);

        var manejada = await sut.TryHandleAsync(ctx, new OperationCanceledException(), CancellationToken.None);

        manejada.Should().BeTrue();
        ctx.Response.StatusCode.Should().Be(200);   // no se cambia: el cliente ya se fue
        Cuerpo(ctx).Should().BeEmpty();
    }

    [Fact]
    public async Task TryHandleAsync_CancelacionSinQueElClienteSeFuera_SeTrataComoError500()
    {
        var ctx = Contexto();

        await sut.TryHandleAsync(ctx, new OperationCanceledException(), CancellationToken.None);

        ctx.Response.StatusCode.Should().Be(500);
    }
}
