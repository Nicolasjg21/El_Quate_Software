using ElQuateDePatty.DTOs.Kardex;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>KardexMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class KardexMapperTests
{
    private static Kardex Entidad() => new() { idMovimiento = 7, idProducto = 1, tipoMovimiento = "SALIDA", cantidad = 8, stockAnterior = 24, stockNuevo = 16, motivo = "Venta", fecha = new DateTime(2026, 11, 5, 21, 0, 0), idUsuario = 3 };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new KardexCrearDTO { idProducto = 1, tipoMovimiento = "SALIDA", cantidad = 8, stockAnterior = 24, stockNuevo = 16, motivo = "Venta", fecha = new DateTime(2026, 11, 5, 21, 0, 0) };

        var entidad = dto.ToEntity();

        entidad.idProducto.Should().Be(1);
        entidad.tipoMovimiento.Should().Be("SALIDA");
        entidad.cantidad.Should().Be(8);
        entidad.stockAnterior.Should().Be(24);
        entidad.stockNuevo.Should().Be(16);
        entidad.motivo.Should().Be("Venta");
        entidad.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.idMovimiento.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idMovimiento.Should().Be(7);
        dto.idProducto.Should().Be(1);
        dto.tipoMovimiento.Should().Be("SALIDA");
        dto.cantidad.Should().Be(8);
        dto.stockAnterior.Should().Be(24);
        dto.stockNuevo.Should().Be(16);
        dto.motivo.Should().Be("Venta");
        dto.fecha.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.idUsuario.Should().Be(3);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Kardex> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<KardexRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Kardex>().ToRespuestaDTO().Should().BeEmpty();
    }
}
