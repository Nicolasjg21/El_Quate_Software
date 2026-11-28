using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Productos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ProductoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ProductoCrearDTOValidacionTests
{
    private static ProductoCrearDTO Valido() => new() { nombreProducto = "Cerveza Águila", precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1 };

    [Fact]
    public void ProductoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreProducto_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProducto = "";

        Validar.Errores(dto).Should().Contain("nombreProducto");
    }

    [Fact]
    public void nombreProducto_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProducto = "   ";

        Validar.Errores(dto).Should().Contain("nombreProducto");
    }

    [Fact]
    public void nombreProducto_200Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreProducto = new string('a', 200);

        Validar.Errores(dto).Should().NotContain("nombreProducto");
    }

    [Fact]
    public void nombreProducto_201Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProducto = new string('a', 201);

        Validar.Errores(dto).Should().Contain("nombreProducto");
    }

    [Fact]
    public void precioVenta_Cero_EsValido()
    {
        var dto = Valido();
        dto.precioVenta = 0m;

        Validar.Errores(dto).Should().NotContain("precioVenta");
    }

    [Fact]
    public void precioVenta_Negativo_EsInvalido()
    {
        var dto = Valido();
        dto.precioVenta = -1m;

        Validar.Errores(dto).Should().Contain("precioVenta");
    }

    [Fact]
    public void precioVenta_Maximo_EsValido()
    {
        var dto = Valido();
        dto.precioVenta = 99999999m;

        Validar.Errores(dto).Should().NotContain("precioVenta");
    }

    [Fact]
    public void precioVenta_SobreElMaximo_EsInvalido()
    {
        var dto = Valido();
        dto.precioVenta = 100000000m;

        Validar.Errores(dto).Should().Contain("precioVenta");
    }

    [Fact]
    public void precioVenta_ConCentavos_EsInvalido()
    {
        var dto = Valido();
        dto.precioVenta = 1500.50m;

        Validar.Errores(dto).Should().Contain("precioVenta");
    }

    [Fact]
    public void cantidadMinima_Igual0_EsValido()
    {
        var dto = Valido();
        dto.cantidadMinima = 0;

        Validar.Errores(dto).Should().NotContain("cantidadMinima");
    }

    [Fact]
    public void cantidadMinima_IgualMenos1_EsInvalido()
    {
        var dto = Valido();
        dto.cantidadMinima = -1;

        Validar.Errores(dto).Should().Contain("cantidadMinima");
    }

    [Fact]
    public void idCategoria_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idCategoria = 1;

        Validar.Errores(dto).Should().NotContain("idCategoria");
    }

    [Fact]
    public void idCategoria_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idCategoria = 0;

        Validar.Errores(dto).Should().Contain("idCategoria");
    }
}
