using System.Globalization;
using ElQuateDePatty.DTOs.Validaciones;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>CB-10 a CB-12 · Regla RN-04: importes en pesos sin centavos. Un caso por rama del switch.</summary>
public class PesosEnterosAttributeTests
{
    private readonly PesosEnterosAttribute sut = new();

    // CB-10 · Rama "null": la obligatoriedad la decide [Required], no este atributo.
    [Fact]
    public void IsValid_Nulo_EsValido()
    {
        sut.IsValid(null).Should().BeTrue();
    }

    // CB-11 · Rama "decimal", verdadero y falso.
    [Theory]
    [InlineData("1500", true)]
    [InlineData("0", true)]
    [InlineData("1500.00", true)]       // ceros después del punto: sigue siendo entero
    [InlineData("1500.50", false)]
    [InlineData("0.01", false)]         // el centavo más pequeño
    public void IsValid_Decimal_SoloAceptaPesosSinCentavos(string valor, bool esperado)
    {
        // Los atributos de C# no admiten decimal como argumento: se escribe como texto y se convierte.
        var importe = decimal.Parse(valor, CultureInfo.InvariantCulture);

        sut.IsValid(importe).Should().Be(esperado);
    }

    // CB-12 · Rama "_": cualquier tipo que no sea decimal. Hallazgo documentado en el informe.
    [Fact]
    public void IsValid_EnteroQueNoEsDecimal_EsInvalido()
    {
        sut.IsValid(1500).Should().BeFalse();   // 1500 es int, no decimal
    }
}
