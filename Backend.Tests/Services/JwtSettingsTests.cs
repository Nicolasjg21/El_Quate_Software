using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>CB-13 a CB-15 · La API no arranca con una configuración JWT insegura.</summary>
public class JwtSettingsTests
{
    private const string clave32 = "12345678901234567890123456789012";   // 32 caracteres ASCII = 32 bytes

    private static JwtSettings Crear(string key = clave32, string issuer = "ElQuateDePatty", int minutos = 30) =>
        new() { key = key, issuer = issuer, audience = "ElQuateDePatty", expirationMinutes = minutos };

    // CB-13 · Por debajo del límite de 32 bytes.
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validar_ClaveDeMenosDe32Bytes_LanzaExcepcion(int bytes)
    {
        var sut = Crear(key: new string('k', bytes));

        sut.Invoking(s => s.Validar()).Should().Throw<InvalidOperationException>()
           .WithMessage("*32 bytes*");
    }

    // CB-13 · En el límite y por encima.
    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void Validar_ClaveDe32BytesOMas_NoLanza(int bytes)
    {
        Crear(key: new string('k', bytes)).Invoking(s => s.Validar()).Should().NotThrow();
    }

    // Se cuentan bytes UTF-8, no caracteres: 16 "ñ" = 32 bytes.
    [Fact]
    public void Validar_ClaveConCaracteresDeDosBytes_CuentaBytes()
    {
        Crear(key: new string('ñ', 16)).Invoking(s => s.Validar()).Should().NotThrow();
    }

    // CB-14 · Issuer vacío.
    [Fact]
    public void Validar_IssuerVacio_LanzaExcepcion()
    {
        Crear(issuer: "  ").Invoking(s => s.Validar()).Should().Throw<InvalidOperationException>();
    }

    // CB-15 · Expiración fuera del rango 1..1440.
    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void Validar_ExpiracionFueraDeRango_LanzaExcepcion(int minutos)
    {
        Crear(minutos: minutos).Invoking(s => s.Validar()).Should().Throw<InvalidOperationException>()
            .WithMessage("*ExpirationMinutes*");
    }

    // CB-15 · En los bordes del rango.
    [Theory]
    [InlineData(1)]
    [InlineData(1440)]
    public void Validar_ExpiracionEnLosBordes_NoLanza(int minutos)
    {
        Crear(minutos: minutos).Invoking(s => s.Validar()).Should().NotThrow();
    }
}
