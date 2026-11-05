using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>MesasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class MesasMapperTests
{
    private static Mesas Entidad() => new() { idMesa = 7, numeroMesa = 3, estado = "Ocupada" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new MesaCrearDTO { numeroMesa = 3, estado = "Ocupada" };

        var entidad = dto.ToEntity();

        entidad.numeroMesa.Should().Be(3);
        entidad.estado.Should().Be("Ocupada");
        entidad.idMesa.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idMesa.Should().Be(7);
        dto.numeroMesa.Should().Be(3);
        dto.estado.Should().Be("Ocupada");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Mesas> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<MesaRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Mesas>().ToRespuestaDTO().Should().BeEmpty();
    }
}
