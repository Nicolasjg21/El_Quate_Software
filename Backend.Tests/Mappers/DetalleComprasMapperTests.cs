using ElQuateDePatty.DTOs.DetalleCompras;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>DetalleComprasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class DetalleComprasMapperTests
{
    private static DetalleCompras Entidad() => new() { idDetalleCompra = 7, idCompra = 1, idProducto = 3, cantidad = 12, precioCompra = 4000m };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new DetalleCompraCrearDTO { idCompra = 1, idProducto = 3, cantidad = 12, precioCompra = 4000m };

        var entidad = dto.ToEntity();

        entidad.idCompra.Should().Be(1);
        entidad.idProducto.Should().Be(3);
        entidad.cantidad.Should().Be(12);
        entidad.precioCompra.Should().Be(4000m);
        entidad.idDetalleCompra.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idDetalleCompra.Should().Be(7);
        dto.idCompra.Should().Be(1);
        dto.idProducto.Should().Be(3);
        dto.cantidad.Should().Be(12);
        dto.precioCompra.Should().Be(4000m);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<DetalleCompras> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<DetalleCompraRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<DetalleCompras>().ToRespuestaDTO().Should().BeEmpty();
    }
}
