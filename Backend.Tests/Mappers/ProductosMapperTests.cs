using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>ProductosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class ProductosMapperTests
{
    private static Productos Entidad() => new() { idProducto = 7, nombreProducto = "Cerveza Águila", precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1 };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new ProductoCrearDTO { nombreProducto = "Cerveza Águila", precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1 };

        var entidad = dto.ToEntity();

        entidad.nombreProducto.Should().Be("Cerveza Águila");
        entidad.precioVenta.Should().Be(5000m);
        entidad.cantidadMinima.Should().Be(10);
        entidad.estado.Should().Be(true);
        entidad.idCategoria.Should().Be(1);
        entidad.idProducto.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idProducto.Should().Be(7);
        dto.nombreProducto.Should().Be("Cerveza Águila");
        dto.precioVenta.Should().Be(5000m);
        dto.cantidadMinima.Should().Be(10);
        dto.estado.Should().Be(true);
        dto.idCategoria.Should().Be(1);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Productos> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<ProductoDetalleRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Productos>().ToRespuestaDTO().Should().BeEmpty();
    }
}
