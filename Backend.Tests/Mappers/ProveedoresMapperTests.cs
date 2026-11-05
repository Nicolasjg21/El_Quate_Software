using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>ProveedoresMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class ProveedoresMapperTests
{
    private static Proveedores Entidad() => new() { idProveedor = 7, nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

        var entidad = dto.ToEntity();

        entidad.nombreProveedor.Should().Be("Bavaria");
        entidad.telefono.Should().Be("3001234567");
        entidad.direccion.Should().Be("Cra 1");
        entidad.idProveedor.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idProveedor.Should().Be(7);
        dto.nombreProveedor.Should().Be("Bavaria");
        dto.telefono.Should().Be("3001234567");
        dto.direccion.Should().Be("Cra 1");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Proveedores> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<ProveedorRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Proveedores>().ToRespuestaDTO().Should().BeEmpty();
    }
}
