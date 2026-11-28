using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>CB-07 a CB-09 · Bloqueo de login tras 5 fallos (valores límite y transición de estados).</summary>
public class RastreadorIntentosLoginTests
{
    private readonly RastreadorIntentosLogin sut = new(new MemoryCache(new MemoryCacheOptions()));

    // CB-07 · Valor límite: 4 no bloquea, 5 bloquea.
    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public void EstaBloqueado_SegunFallos_RetornaEsperado(int fallos, bool esperado)
    {
        // Arrange: el bucle solo prepara el escenario; la verificación no tiene lógica.
        for (var i = 0; i < fallos; i++) sut.RegistrarFallo("laura@elcuate.com");

        // Act + Assert
        sut.EstaBloqueado("laura@elcuate.com").Should().Be(esperado);
    }

    // CB-08 · Mayúsculas y espacios cuentan como el mismo correo.
    [Fact]
    public void EstaBloqueado_MayusculasYEspacios_CuentaComoElMismoCorreo()
    {
        for (var i = 0; i < 5; i++) sut.RegistrarFallo("LAURA@elcuate.com ");

        sut.EstaBloqueado("laura@elcuate.com").Should().BeTrue();
    }

    // CB-09 · Un login correcto limpia el contador y desbloquea.
    [Fact]
    public void Limpiar_DespuesDeFallos_Desbloquea()
    {
        for (var i = 0; i < 5; i++) sut.RegistrarFallo("laura@elcuate.com");

        sut.Limpiar("laura@elcuate.com");

        sut.EstaBloqueado("laura@elcuate.com").Should().BeFalse();
    }

    // Partición: un correo nunca usado no está bloqueado.
    [Fact]
    public void EstaBloqueado_CorreoSinFallos_NoEstaBloqueado()
    {
        sut.EstaBloqueado("nuevo@elcuate.com").Should().BeFalse();
    }
}
