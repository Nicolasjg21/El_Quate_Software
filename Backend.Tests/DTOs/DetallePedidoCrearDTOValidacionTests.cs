using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.DetallePedidos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de DetallePedidoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class DetallePedidoCrearDTOValidacionTests
{
    private static DetallePedidoCrearDTO Valido() => new() { idPedido = 1, idProducto = 1, cantidad = 2, precioUnitario = 5000m };

    [Fact]
    public void DetallePedidoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idPedido_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idPedido = 1;

        Validar.Errores(dto).Should().NotContain("idPedido");
    }

    [Fact]
    public void idPedido_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idPedido = 0;

        Validar.Errores(dto).Should().Contain("idPedido");
    }

    [Fact]
    public void idProducto_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idProducto = 1;

        Validar.Errores(dto).Should().NotContain("idProducto");
    }

    [Fact]
    public void idProducto_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idProducto = 0;

        Validar.Errores(dto).Should().Contain("idProducto");
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

    [Fact]
    public void precioUnitario_Cero_EsValido()
    {
        var dto = Valido();
        dto.precioUnitario = 0m;

        Validar.Errores(dto).Should().NotContain("precioUnitario");
    }

    [Fact]
    public void precioUnitario_Negativo_EsInvalido()
    {
        var dto = Valido();
        dto.precioUnitario = -0.01m;

        Validar.Errores(dto).Should().Contain("precioUnitario");
    }

    [Fact]
    public void precioUnitario_Maximo_EsValido()
    {
        var dto = Valido();
        dto.precioUnitario = 99999999.99m;

        Validar.Errores(dto).Should().NotContain("precioUnitario");
    }

    [Fact]
    public void precioUnitario_SobreElMaximo_EsInvalido()
    {
        var dto = Valido();
        dto.precioUnitario = 100000000m;

        Validar.Errores(dto).Should().Contain("precioUnitario");
    }
}
