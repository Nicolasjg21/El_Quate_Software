using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Kardex;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de KardexCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class KardexCrearDTOValidacionTests
{
    private static KardexCrearDTO Valido() => new() { idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, motivo = "Compra", fecha = new DateTime(2026, 11, 5) };

    [Fact]
    public void KardexCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idProducto_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idProducto = 1;

        Validar.Errores(dto).Should().NotContain("idProducto");
    }

    [Fact]
    public void idProducto_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idProducto = 0;

        Validar.Errores(dto).Should().Contain("idProducto");
    }

    [Fact]
    public void tipoMovimiento_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.tipoMovimiento = "";

        Validar.Errores(dto).Should().Contain("tipoMovimiento");
    }

    [Fact]
    public void tipoMovimiento_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.tipoMovimiento = "   ";

        Validar.Errores(dto).Should().Contain("tipoMovimiento");
    }

    [Fact]
    public void tipoMovimiento_10Caracteres_EsValido()
    {
        var dto = Valido();
        dto.tipoMovimiento = new string('a', 10);

        Validar.Errores(dto).Should().NotContain("tipoMovimiento");
    }

    [Fact]
    public void tipoMovimiento_11Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.tipoMovimiento = new string('a', 11);

        Validar.Errores(dto).Should().Contain("tipoMovimiento");
    }

    [Fact]
    public void motivo_500Caracteres_EsValido()
    {
        var dto = Valido();
        dto.motivo = new string('a', 500);

        Validar.Errores(dto).Should().NotContain("motivo");
    }

    [Fact]
    public void motivo_501Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.motivo = new string('a', 501);

        Validar.Errores(dto).Should().Contain("motivo");
    }

    [Fact]
    public void fecha_2000_01_01_EsValido()
    {
        var dto = Valido();
        dto.fecha = new DateTime(2000, 1, 1);

        Validar.Errores(dto).Should().NotContain("fecha");
    }

    [Fact]
    public void fecha_1999_12_31_EsInvalido()
    {
        var dto = Valido();
        dto.fecha = new DateTime(1999, 12, 31);

        Validar.Errores(dto).Should().Contain("fecha");
    }

    [Fact]
    public void fecha_2100_12_31_EsValido()
    {
        var dto = Valido();
        dto.fecha = new DateTime(2100, 12, 31);

        Validar.Errores(dto).Should().NotContain("fecha");
    }

    [Fact]
    public void fecha_2101_01_01_EsInvalido()
    {
        var dto = Valido();
        dto.fecha = new DateTime(2101, 1, 1);

        Validar.Errores(dto).Should().Contain("fecha");
    }
}
