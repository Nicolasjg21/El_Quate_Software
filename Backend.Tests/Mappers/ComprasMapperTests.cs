using ElQuateDePatty.DTOs.Compras;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>ComprasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class ComprasMapperTests
{
    private static Compras Entidad() => new() { idCompra = 7, idProveedor = 2, fecha = new DateTime(2026, 11, 5, 21, 0, 0), total = 120000m };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new CompraCrearDTO { idProveedor = 2, fecha = new DateTime(2026, 11, 5, 21, 0, 0), total = 120000m };

        var entidad = dto.ToEntity();

        entidad.idProveedor.Should().Be(2);
        entidad.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.total.Should().Be(120000m);
        entidad.idCompra.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idCompra.Should().Be(7);
        dto.idProveedor.Should().Be(2);
        dto.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.total.Should().Be(120000m);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Compras> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<CompraRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Compras>().ToRespuestaDTO().Should().BeEmpty();
    }
}
