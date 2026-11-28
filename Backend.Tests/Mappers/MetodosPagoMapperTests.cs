using ElQuateDePatty.DTOs.MetodosPago;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>MetodosPagoMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class MetodosPagoMapperTests
{
    private static MetodosPago Entidad() => new() { idMetodo = 7, nombreMetodo = "Nequi" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new MetodoPagoCrearDTO { nombreMetodo = "Nequi" };

        var entidad = dto.ToEntity();

        entidad.nombreMetodo.Should().Be("Nequi");
        entidad.idMetodo.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idMetodo.Should().Be(7);
        dto.nombreMetodo.Should().Be("Nequi");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<MetodosPago> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<MetodoPagoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<MetodosPago>().ToRespuestaDTO().Should().BeEmpty();
    }
}
