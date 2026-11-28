using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>Logout: una sesión revocada no se puede volver a usar.</summary>
public class RevocacionTokenServiceTests
{
    private readonly RevocacionTokenService sut = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void EstaRevocada_SesionNueva_NoEstaRevocada()
    {
        sut.EstaRevocada("sesion-1").Should().BeFalse();
    }

    [Fact]
    public void Revocar_TokenVigente_QuedaRevocada()
    {
        sut.Revocar("sesion-1", DateTimeOffset.UtcNow.AddMinutes(30));

        sut.EstaRevocada("sesion-1").Should().BeTrue();
    }

    [Fact]
    public void Revocar_TokenYaVencido_NoSeGuarda()
    {
        // Rama "if (expiraEn <= ahora) return": no tiene sentido guardar un token que ya no sirve.
        sut.Revocar("sesion-vieja", DateTimeOffset.UtcNow.AddMinutes(-1));

        sut.EstaRevocada("sesion-vieja").Should().BeFalse();
    }

    [Fact]
    public void Revocar_UnaSesion_NoAfectaOtras()
    {
        sut.Revocar("sesion-1", DateTimeOffset.UtcNow.AddMinutes(30));

        sut.EstaRevocada("sesion-2").Should().BeFalse();
    }
}
