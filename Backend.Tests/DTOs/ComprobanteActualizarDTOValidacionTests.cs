using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Comprobantes;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ComprobanteActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ComprobanteActualizarDTOValidacionTests
{
    private static ComprobanteActualizarDTO Valido() => new() { idComprobante = 1, idCuenta = 1, fecha = new DateTime(2026, 11, 5), total = 50000m, idMetodo = 1 };

    [Fact]
    public void ComprobanteActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
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
