using ElQuateDePatty.DTOs.RolesPermisos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>RolesPermisosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class RolesPermisosMapperTests
{
    private static RolesPermisos Entidad() => new() { idRol = 1, idPermiso = 2 };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new RolPermisoCrearDTO { idRol = 1, idPermiso = 2 };

        var entidad = dto.ToEntity();

        entidad.idRol.Should().Be(1);
        entidad.idPermiso.Should().Be(2);
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idRol.Should().Be(1);
        dto.idPermiso.Should().Be(2);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<RolesPermisos> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<RolPermisoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<RolesPermisos>().ToRespuestaDTO().Should().BeEmpty();
    }
}
