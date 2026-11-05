using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Cuentas;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CuentaCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CuentaCrearDTOValidacionTests
{
    private static CuentaCrearDTO Valido() => new() { idMesa = 1, estado = "Abierta", fechaApertura = new DateTime(2026, 11, 5), total = 0m };

    [Fact]
    public void CuentaCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idMesa_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idMesa = 1;

        Validar.Errores(dto).Should().NotContain("idMesa");
    }

    [Fact]
    public void idMesa_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idMesa = 0;

        Validar.Errores(dto).Should().Contain("idMesa");
    }

    [Fact]
    public void estado_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.estado = "";

        Validar.Errores(dto).Should().Contain("estado");
    }

    [Fact]
    public void estado_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.estado = "   ";

        Validar.Errores(dto).Should().Contain("estado");
    }

    [Fact]
    public void estado_20Caracteres_EsValido()
    {
        var dto = Valido();
        dto.estado = new string('a', 20);

        Validar.Errores(dto).Should().NotContain("estado");
    }

    [Fact]
    public void estado_21Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.estado = new string('a', 21);

        Validar.Errores(dto).Should().Contain("estado");
    }

    [Fact]
    public void fechaApertura_2000_01_01_EsValido()
    {
        var dto = Valido();
        dto.fechaApertura = new DateTime(2000, 1, 1);

        Validar.Errores(dto).Should().NotContain("fechaApertura");
    }

    [Fact]
    public void fechaApertura_1999_12_31_EsInvalido()
    {
        var dto = Valido();
        dto.fechaApertura = new DateTime(1999, 12, 31);

        Validar.Errores(dto).Should().Contain("fechaApertura");
    }

    [Fact]
    public void fechaApertura_2100_12_31_EsValido()
    {
        var dto = Valido();
        dto.fechaApertura = new DateTime(2100, 12, 31);

        Validar.Errores(dto).Should().NotContain("fechaApertura");
    }

    [Fact]
    public void fechaApertura_2101_01_01_EsInvalido()
    {
        var dto = Valido();
        dto.fechaApertura = new DateTime(2101, 1, 1);

        Validar.Errores(dto).Should().Contain("fechaApertura");
    }

    [Fact]
    public void fechaCierre_2000_01_01_EsValido()
    {
        var dto = Valido();
        dto.fechaCierre = new DateTime(2000, 1, 1);

        Validar.Errores(dto).Should().NotContain("fechaCierre");
    }

    [Fact]
    public void fechaCierre_1999_12_31_EsInvalido()
    {
        var dto = Valido();
        dto.fechaCierre = new DateTime(1999, 12, 31);

        Validar.Errores(dto).Should().Contain("fechaCierre");
    }

    [Fact]
    public void fechaCierre_2100_12_31_EsValido()
    {
        var dto = Valido();
        dto.fechaCierre = new DateTime(2100, 12, 31);

        Validar.Errores(dto).Should().NotContain("fechaCierre");
    }

    [Fact]
    public void fechaCierre_2101_01_01_EsInvalido()
    {
        var dto = Valido();
        dto.fechaCierre = new DateTime(2101, 1, 1);

        Validar.Errores(dto).Should().Contain("fechaCierre");
    }

    [Fact]
    public void fechaCierre_Nula_EsValido()
    {
        var dto = Valido();
        dto.fechaCierre = null;

        Validar.Errores(dto).Should().NotContain("fechaCierre");
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
}
