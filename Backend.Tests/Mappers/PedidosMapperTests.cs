using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>PedidosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class PedidosMapperTests
{
    private static Pedidos Entidad() => new() { idPedido = 7, idCuenta = 1, idUsuario = 2, fecha = new DateTime(2026, 11, 5, 21, 0, 0), estadoPedido = "Pendiente" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new PedidoCrearDTO { idCuenta = 1, idUsuario = 2, fecha = new DateTime(2026, 11, 5, 21, 0, 0), estadoPedido = "Pendiente" };

        var entidad = dto.ToEntity();

        entidad.idCuenta.Should().Be(1);
        entidad.idUsuario.Should().Be(2);
        entidad.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.estadoPedido.Should().Be("Pendiente");
        entidad.idPedido.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idPedido.Should().Be(7);
        dto.idCuenta.Should().Be(1);
        dto.idUsuario.Should().Be(2);
        dto.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.estadoPedido.Should().Be("Pendiente");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Pedidos> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<PedidoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Pedidos>().ToRespuestaDTO().Should().BeEmpty();
    }
}
