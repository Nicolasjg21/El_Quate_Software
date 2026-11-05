using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>ComprobantesMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class ComprobantesMapperTests
{
    private static Comprobantes Entidad() => new() { idComprobante = 7, idCuenta = 3, fecha = new DateTime(2026, 11, 5, 21, 0, 0), total = 90000m, idMetodo = 2 };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new ComprobanteCrearDTO { idCuenta = 3, fecha = new DateTime(2026, 11, 5, 21, 0, 0), total = 90000m, idMetodo = 2 };

        var entidad = dto.ToEntity();

        entidad.idCuenta.Should().Be(3);
        entidad.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.total.Should().Be(90000m);
        entidad.idMetodo.Should().Be(2);
        entidad.idComprobante.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idComprobante.Should().Be(7);
        dto.idCuenta.Should().Be(3);
        dto.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.total.Should().Be(90000m);
        dto.idMetodo.Should().Be(2);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Comprobantes> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<ComprobanteRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Comprobantes>().ToRespuestaDTO().Should().BeEmpty();
    }
}
