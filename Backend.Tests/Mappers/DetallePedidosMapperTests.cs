using ElQuateDePatty.DTOs.DetallePedidos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>DetallePedidosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class DetallePedidosMapperTests
{
    private static DetallePedidos Entidad() => new() { idDetalle = 7, idPedido = 1, idProducto = 2, cantidad = 3, precioUnitario = 60000m };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new DetallePedidoCrearDTO { idPedido = 1, idProducto = 2, cantidad = 3, precioUnitario = 60000m };

        var entidad = dto.ToEntity();

        entidad.idPedido.Should().Be(1);
        entidad.idProducto.Should().Be(2);
        entidad.cantidad.Should().Be(3);
        entidad.precioUnitario.Should().Be(60000m);
        entidad.idDetalle.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idDetalle.Should().Be(7);
        dto.idPedido.Should().Be(1);
        dto.idProducto.Should().Be(2);
        dto.cantidad.Should().Be(3);
        dto.precioUnitario.Should().Be(60000m);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<DetallePedidos> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<DetallePedidoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<DetallePedidos>().ToRespuestaDTO().Should().BeEmpty();
    }
}
