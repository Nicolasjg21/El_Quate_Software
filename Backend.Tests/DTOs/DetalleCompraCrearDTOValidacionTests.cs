using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.DetalleCompras;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de DetalleCompraCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class DetalleCompraCrearDTOValidacionTests
{
    private static DetalleCompraCrearDTO Valido() => new() { idCompra = 1, idProducto = 1, cantidad = 24, precioCompra = 3000m };

    [Fact]
    public void DetalleCompraCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idCompra_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idCompra = 1;

        Validar.Errores(dto).Should().NotContain("idCompra");
    }

    [Fact]
    public void idCompra_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idCompra = 0;

        Validar.Errores(dto).Should().Contain("idCompra");
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
    public void precioCompra_Cero_EsValido()
    {
        var dto = Valido();
        dto.precioCompra = 0m;

        Validar.Errores(dto).Should().NotContain("precioCompra");
    }

    [Fact]
    public void precioCompra_Negativo_EsInvalido()
    {
        var dto = Valido();
        dto.precioCompra = -0.01m;

        Validar.Errores(dto).Should().Contain("precioCompra");
    }

    [Fact]
    public void precioCompra_Maximo_EsValido()
    {
        var dto = Valido();
        dto.precioCompra = 99999999.99m;

        Validar.Errores(dto).Should().NotContain("precioCompra");
    }

    [Fact]
    public void precioCompra_SobreElMaximo_EsInvalido()
    {
        var dto = Valido();
        dto.precioCompra = 100000000m;

        Validar.Errores(dto).Should().Contain("precioCompra");
    }
}
