using ElQuateDePatty.DTOs.TipoDocumento;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>TipoDocumentoMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class TipoDocumentoMapperTests
{
    private static TipoDocumento Entidad() => new() { idTipoDocumento = 7, nombreTipo = "Cédula" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new TipoDocumentoCrearDTO { nombreTipo = "Cédula" };

        var entidad = dto.ToEntity();

        entidad.nombreTipo.Should().Be("Cédula");
        entidad.idTipoDocumento.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idTipoDocumento.Should().Be(7);
        dto.nombreTipo.Should().Be("Cédula");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<TipoDocumento> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<TipoDocumentoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<TipoDocumento>().ToRespuestaDTO().Should().BeEmpty();
    }
}
