using ElQuateDePatty.Services;
using FluentAssertions;

namespace Backend.Tests.Services;

/// <summary>
/// Caja blanca de OrigenesCors.Resolver: cubre los 3 caminos (hay orígenes, Development sin orígenes,
/// otro entorno sin orígenes). Confirma que nunca se permite "cualquier origen" (Sonar S5122).
/// </summary>
public class OrigenesCorsTests
{
    [Fact]
    public void Resolver_ConOrigenesConfigurados_LosDevuelveSinVacios()
    {
        var resultado = OrigenesCors.Resolver(["https://elquate.com", "", "  ", "http://localhost:5500"], esDesarrollo: false);

        resultado.Should().Equal("https://elquate.com", "http://localhost:5500");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resolver_ConOrigenesConfigurados_IgnoraElEntorno(bool esDesarrollo)
    {
        var resultado = OrigenesCors.Resolver(["https://elquate.com"], esDesarrollo);

        resultado.Should().Equal("https://elquate.com");
    }

    [Fact]
    public void Resolver_DevelopmentSinConfiguracion_UsaOrigenesLocales()
    {
        var resultado = OrigenesCors.Resolver(null, esDesarrollo: true);

        resultado.Should().Equal("http://localhost:5500", "http://127.0.0.1:5500");
    }

    [Fact]
    public void Resolver_DevelopmentSoloConVacios_UsaOrigenesLocales()
    {
        var resultado = OrigenesCors.Resolver(["", " "], esDesarrollo: true);

        resultado.Should().Equal("http://localhost:5500", "http://127.0.0.1:5500");
    }

    [Fact]
    public void Resolver_ProduccionSinConfiguracion_LanzaExcepcion()
    {
        var accion = () => OrigenesCors.Resolver(null, esDesarrollo: false);

        accion.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cors:AllowedOrigins no está configurado*");
    }

    [Fact]
    public void Resolver_ProduccionConListaVacia_LanzaExcepcion()
    {
        var accion = () => OrigenesCors.Resolver([], esDesarrollo: false);

        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolver_DevuelveCopia_NoSePuedeAlterarLaListaInterna()
    {
        var primera = OrigenesCors.Resolver(null, esDesarrollo: true);
        primera[0] = "https://atacante.com";

        OrigenesCors.Resolver(null, esDesarrollo: true).Should().NotContain("https://atacante.com");
    }

    [Fact]
    public void Resolver_NuncaDevuelveComodin()
    {
        OrigenesCors.Resolver(null, esDesarrollo: true).Should().NotContain("*");
    }
}