using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.MetodosPago;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de MetodoPagoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class MetodoPagoCrearDTOValidacionTests
{
    private static MetodoPagoCrearDTO Valido() => new() { nombreMetodo = "Efectivo" };

    [Fact]
    public void MetodoPagoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreMetodo_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreMetodo = "";

        Validar.Errores(dto).Should().Contain("nombreMetodo");
    }

    [Fact]
    public void nombreMetodo_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreMetodo = "   ";

        Validar.Errores(dto).Should().Contain("nombreMetodo");
    }

    [Fact]
    public void nombreMetodo_50Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreMetodo = new string('a', 50);

        Validar.Errores(dto).Should().NotContain("nombreMetodo");
    }

    [Fact]
    public void nombreMetodo_51Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreMetodo = new string('a', 51);

        Validar.Errores(dto).Should().Contain("nombreMetodo");
    }
}
