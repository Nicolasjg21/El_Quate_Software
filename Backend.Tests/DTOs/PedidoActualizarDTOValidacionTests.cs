using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Pedidos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de PedidoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class PedidoActualizarDTOValidacionTests
{
    private static PedidoActualizarDTO Valido() => new() { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" };

    [Fact]
    public void PedidoActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void estadoPedido_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.estadoPedido = "";

        Validar.Errores(dto).Should().Contain("estadoPedido");
    }

    [Fact]
    public void estadoPedido_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.estadoPedido = "   ";

        Validar.Errores(dto).Should().Contain("estadoPedido");
    }
}
