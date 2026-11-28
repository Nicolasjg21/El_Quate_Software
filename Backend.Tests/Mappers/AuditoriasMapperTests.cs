using ElQuateDePatty.DTOs.Auditorias;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>AuditoriasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class AuditoriasMapperTests
{
    private static Auditorias Entidad() => new() { idAuditoria = 7, tabla = "Productos", accion = "UPDATE", fecha = new DateTime(2026, 11, 5, 21, 0, 0), datosAnteriores = "precio=5000", datosNuevos = "precio=5500", idUsuario = 3 };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new AuditoriaCrearDTO { tabla = "Productos", accion = "UPDATE", fecha = new DateTime(2026, 11, 5, 21, 0, 0), datosAnteriores = "precio=5000", datosNuevos = "precio=5500" };

        var entidad = dto.ToEntity();

        entidad.tabla.Should().Be("Productos");
        entidad.accion.Should().Be("UPDATE");
        entidad.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.datosAnteriores.Should().Be("precio=5000");
        entidad.datosNuevos.Should().Be("precio=5500");
        entidad.idAuditoria.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idAuditoria.Should().Be(7);
        dto.tabla.Should().Be("Productos");
        dto.accion.Should().Be("UPDATE");
        dto.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.datosAnteriores.Should().Be("precio=5000");
        dto.datosNuevos.Should().Be("precio=5500");
        dto.idUsuario.Should().Be(3);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Auditorias> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<AuditoriaRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Auditorias>().ToRespuestaDTO().Should().BeEmpty();
    }
}
