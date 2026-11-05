using System.Security.Claims;
using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>Quién es administrador según el rol del token.</summary>
public class AutorizacionSettingsTests
{
    private readonly AutorizacionSettings sut = new() { rolesAdministrador = new HashSet<string> { "1" } };

    private static ClaimsPrincipal ConRol(string? rol) =>
        new(new ClaimsIdentity(rol is null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.Role, rol) }, "Prueba"));

    [Fact]
    public void EsAdministrador_RolAdministrador_RetornaTrue()
    {
        sut.EsAdministrador(ConRol("1")).Should().BeTrue();
    }

    [Fact]
    public void EsAdministrador_OtroRol_RetornaFalse()
    {
        sut.EsAdministrador(ConRol("2")).Should().BeFalse();
    }

    [Fact]
    public void EsAdministrador_SinRol_RetornaFalse()
    {
        sut.EsAdministrador(ConRol(null)).Should().BeFalse();
    }
}
