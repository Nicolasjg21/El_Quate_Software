using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.DetalleCompras;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de DetalleCompraActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class DetalleCompraActualizarDTOValidacionTests
{
    private static DetalleCompraActualizarDTO Valido() => new() { idDetalleCompra = 1, idCompra = 1, idProducto = 1, cantidad = 24, precioCompra = 3000m };

    [Fact]
    public void DetalleCompraActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void cantidad_Igual1_EsValido()
    {
        var dto = Valido();
        dto.cantidad = 1;

        Validar.Errores(dto).Should().NotContain("cantidad");
    }

    [Fact]
    public void cantidad_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.cantidad = 0;

        Validar.Errores(dto).Should().Contain("cantidad");
    }
}
