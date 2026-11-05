using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Productos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ProductoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ProductoActualizarDTOValidacionTests
{
    private static ProductoActualizarDTO Valido() => new() { idProducto = 1, nombreProducto = "Cerveza Águila", precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1 };

    [Fact]
    public void ProductoActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
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
}
