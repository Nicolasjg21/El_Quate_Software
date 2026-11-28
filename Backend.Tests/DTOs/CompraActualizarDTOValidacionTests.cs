using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Compras;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CompraActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CompraActualizarDTOValidacionTests
{
    private static CompraActualizarDTO Valido() => new() { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5), total = 120000m };

    [Fact]
    public void CompraActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idProveedor_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idProveedor = 1;

        Validar.Errores(dto).Should().NotContain("idProveedor");
    }

    [Fact]
    public void idProveedor_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idProveedor = 0;

        Validar.Errores(dto).Should().Contain("idProveedor");
    }
}
