using ElQuateDePatty.DTOs.Roles;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>RolesMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class RolesMapperTests
{
    private static Roles Entidad() => new() { idRol = 7, nombreRol = "Mesero" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new RolCrearDTO { nombreRol = "Mesero" };

        var entidad = dto.ToEntity();

        entidad.nombreRol.Should().Be("Mesero");
        entidad.idRol.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idRol.Should().Be(7);
        dto.nombreRol.Should().Be("Mesero");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Roles> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<RolRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Roles>().ToRespuestaDTO().Should().BeEmpty();
    }
}
