using ElQuateDePatty.DTOs.Categorias;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>CategoriasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class CategoriasMapperTests
{
    private static Categorias Entidad() => new() { idCategoria = 7, nombreCategoria = "Cervezas" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new CategoriaCrearDTO { nombreCategoria = "Cervezas" };

        var entidad = dto.ToEntity();

        entidad.nombreCategoria.Should().Be("Cervezas");
        entidad.idCategoria.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idCategoria.Should().Be(7);
        dto.nombreCategoria.Should().Be("Cervezas");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Categorias> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<CategoriaRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Categorias>().ToRespuestaDTO().Should().BeEmpty();
    }
}
