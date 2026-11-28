using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ElQuateDePatty.DTOs.Productos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>RN-04 y rangos del producto: precio en pesos enteros entre 0 y 99.999.999.</summary>
public class ProductoCrearDTOTests
{
    private static ProductoCrearDTO Valido() => new()
    {
        nombreProducto = "Cerveza Águila", precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1
    };

    private static bool EsValido(ProductoCrearDTO dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

    [Theory]
    [InlineData("0", true)]              // límite inferior
    [InlineData("-1", false)]            // por debajo
    [InlineData("99999999", true)]       // límite superior
    [InlineData("100000000", false)]     // por encima
    [InlineData("1500.50", false)]       // con centavos (RN-04)
    public void PrecioVenta_SegunValor_ValidaCorrectamente(string precio, bool esperado)
    {
        var dto = Valido();
        dto.precioVenta = decimal.Parse(precio, CultureInfo.InvariantCulture);

        EsValido(dto).Should().Be(esperado);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IdCategoria_SegunValor_ValidaCorrectamente(int idCategoria, bool esperado)
    {
        var dto = Valido();
        dto.idCategoria = idCategoria;

        EsValido(dto).Should().Be(esperado);
    }

    [Fact]
    public void CantidadMinima_Negativa_EsInvalida()
    {
        var dto = Valido();
        dto.cantidadMinima = -1;

        EsValido(dto).Should().BeFalse();
    }
}
