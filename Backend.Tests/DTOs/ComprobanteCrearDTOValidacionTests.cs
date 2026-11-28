using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Comprobantes;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ComprobanteCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ComprobanteCrearDTOValidacionTests
{
    private static ComprobanteCrearDTO Valido() => new() { idCuenta = 1, fecha = new DateTime(2026, 11, 5), total = 50000m, idMetodo = 1 };

    [Fact]
    public void ComprobanteCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idCuenta_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idCuenta = 1;

        Validar.Errores(dto).Should().NotContain("idCuenta");
    }

    [Fact]
    public void idCuenta_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idCuenta = 0;

        Validar.Errores(dto).Should().Contain("idCuenta");
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

    [Fact]
    public void total_Cero_EsValido()
    {
        var dto = Valido();
        dto.total = 0m;

        Validar.Errores(dto).Should().NotContain("total");
    }

    [Fact]
    public void total_Negativo_EsInvalido()
    {
        var dto = Valido();
        dto.total = -0.01m;

        Validar.Errores(dto).Should().Contain("total");
    }

    [Fact]
    public void total_Maximo_EsValido()
    {
        var dto = Valido();
        dto.total = 99999999.99m;

        Validar.Errores(dto).Should().NotContain("total");
    }

    [Fact]
    public void total_SobreElMaximo_EsInvalido()
    {
        var dto = Valido();
        dto.total = 100000000m;

        Validar.Errores(dto).Should().Contain("total");
    }

    [Fact]
    public void idMetodo_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idMetodo = 1;

        Validar.Errores(dto).Should().NotContain("idMetodo");
    }

    [Fact]
    public void idMetodo_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idMetodo = 0;

        Validar.Errores(dto).Should().Contain("idMetodo");
    }
}
