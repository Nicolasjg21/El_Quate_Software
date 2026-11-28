using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Pedidos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de PedidoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class PedidoCrearDTOValidacionTests
{
    private static PedidoCrearDTO Valido() => new() { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" };

    [Fact]
    public void PedidoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idCuenta_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idCuenta = 1;

        Validar.Errores(dto).Should().NotContain("idCuenta");
    }

    [Fact]
    public void idCuenta_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idCuenta = 0;

        Validar.Errores(dto).Should().Contain("idCuenta");
    }

    [Fact]
    public void idUsuario_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idUsuario = 1;

        Validar.Errores(dto).Should().NotContain("idUsuario");
    }

    [Fact]
    public void idUsuario_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idUsuario = 0;

        Validar.Errores(dto).Should().Contain("idUsuario");
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

    [Fact]
    public void estadoPedido_20Caracteres_EsValido()
    {
        var dto = Valido();
        dto.estadoPedido = new string('a', 20);

        Validar.Errores(dto).Should().NotContain("estadoPedido");
    }

    [Fact]
    public void estadoPedido_21Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.estadoPedido = new string('a', 21);

        Validar.Errores(dto).Should().Contain("estadoPedido");
    }
}
