using ElQuateDePatty.DTOs.Permisos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>PermisosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class PermisosMapperTests
{
    private static Permisos Entidad() => new() { idPermiso = 7, nombrePermiso = "ventas.cobrar" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new PermisoCrearDTO { nombrePermiso = "ventas.cobrar" };

        var entidad = dto.ToEntity();

        entidad.nombrePermiso.Should().Be("ventas.cobrar");
        entidad.idPermiso.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idPermiso.Should().Be(7);
        dto.nombrePermiso.Should().Be("ventas.cobrar");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Permisos> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<PermisoRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Permisos>().ToRespuestaDTO().Should().BeEmpty();
    }
}
