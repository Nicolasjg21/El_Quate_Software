using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.DetallePedidos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de DetallePedidoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class DetallePedidoActualizarDTOValidacionTests
{
    private static DetallePedidoActualizarDTO Valido() => new() { idDetalle = 1, idPedido = 1, idProducto = 1, cantidad = 2, precioUnitario = 5000m };

    [Fact]
    public void DetallePedidoActualizarDTO_DatosCompletos_EsValido()
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
