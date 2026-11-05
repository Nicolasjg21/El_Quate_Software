using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.MetodosPago;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de MetodoPagoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class MetodoPagoActualizarDTOValidacionTests
{
    private static MetodoPagoActualizarDTO Valido() => new() { idMetodo = 1, nombreMetodo = "Efectivo" };

    [Fact]
    public void MetodoPagoActualizarDTO_DatosCompletos_EsValido()
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
}
