using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>El sello cambia cuando cambia la contraseña: así se invalidan los tokens viejos.</summary>
public class SelloSeguridadTests
{
    [Fact]
    public void Calcular_MismoHash_DaMismoSello()
    {
        SelloSeguridad.Calcular("hash-abc").Should().Be(SelloSeguridad.Calcular("hash-abc"));
    }

    [Fact]
    public void Calcular_HashDistinto_DaSelloDistinto()
    {
        SelloSeguridad.Calcular("hash-abc").Should().NotBe(SelloSeguridad.Calcular("hash-abd"));
    }

    [Fact]
    public void Calcular_Siempre_Devuelve32CaracteresHexadecimales()
    {
        // 16 bytes de SHA-256 en hexadecimal = 32 caracteres; no expone el hash original.
        SelloSeguridad.Calcular("hash-abc").Should().MatchRegex("^[0-9A-F]{32}$");
    }
}
