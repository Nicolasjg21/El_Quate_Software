using System.Security.Claims;
using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>ObtenerIdUsuario: lee el id del usuario del token de forma segura.</summary>
public class UsuarioActualExtensionsTests
{
    private static ClaimsPrincipal Con(params Claim[] claims) => new(new ClaimsIdentity(claims, "Prueba"));

    [Fact]
    public void ObtenerIdUsuario_ClaimNumerico_DevuelveElId()
    {
        Con(new Claim(ClaimTypes.NameIdentifier, "7")).ObtenerIdUsuario().Should().Be(7);
    }

    [Fact]
    public void ObtenerIdUsuario_SinClaim_DevuelveNull()
    {
        Con().ObtenerIdUsuario().Should().BeNull();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("7; DROP TABLE Usuarios")]
    public void ObtenerIdUsuario_ClaimNoNumerico_DevuelveNull(string valor)
    {
        Con(new Claim(ClaimTypes.NameIdentifier, valor)).ObtenerIdUsuario().Should().BeNull();
    }
}
