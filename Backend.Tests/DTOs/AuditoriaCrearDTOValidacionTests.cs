using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Auditorias;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de AuditoriaCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class AuditoriaCrearDTOValidacionTests
{
    private static AuditoriaCrearDTO Valido() => new() { tabla = "Productos", accion = "UPDATE", fecha = new DateTime(2026, 11, 5) };

    [Fact]
    public void AuditoriaCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void tabla_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.tabla = "";

        Validar.Errores(dto).Should().Contain("tabla");
    }

    [Fact]
    public void tabla_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.tabla = "   ";

        Validar.Errores(dto).Should().Contain("tabla");
    }

    [Fact]
    public void tabla_50Caracteres_EsValido()
    {
        var dto = Valido();
        dto.tabla = new string('a', 50);

        Validar.Errores(dto).Should().NotContain("tabla");
    }

    [Fact]
    public void tabla_51Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.tabla = new string('a', 51);

        Validar.Errores(dto).Should().Contain("tabla");
    }

    [Fact]
    public void accion_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.accion = "";

        Validar.Errores(dto).Should().Contain("accion");
    }

    [Fact]
    public void accion_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.accion = "   ";

        Validar.Errores(dto).Should().Contain("accion");
    }

    [Fact]
    public void accion_50Caracteres_EsValido()
    {
        var dto = Valido();
        dto.accion = new string('a', 50);

        Validar.Errores(dto).Should().NotContain("accion");
    }

    [Fact]
    public void accion_51Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.accion = new string('a', 51);

        Validar.Errores(dto).Should().Contain("accion");
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
    public void fecha_Nula_EsValido()
    {
        var dto = Valido();
        dto.fecha = null;

        Validar.Errores(dto).Should().NotContain("fecha");
    }

    [Fact]
    public void datosAnteriores_20000Caracteres_EsValido()
    {
        var dto = Valido();
        dto.datosAnteriores = new string('a', 20000);

        Validar.Errores(dto).Should().NotContain("datosAnteriores");
    }

    [Fact]
    public void datosAnteriores_20001Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.datosAnteriores = new string('a', 20001);

        Validar.Errores(dto).Should().Contain("datosAnteriores");
    }

    [Fact]
    public void datosNuevos_20000Caracteres_EsValido()
    {
        var dto = Valido();
        dto.datosNuevos = new string('a', 20000);

        Validar.Errores(dto).Should().NotContain("datosNuevos");
    }

    [Fact]
    public void datosNuevos_20001Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.datosNuevos = new string('a', 20001);

        Validar.Errores(dto).Should().Contain("datosNuevos");
    }
}
