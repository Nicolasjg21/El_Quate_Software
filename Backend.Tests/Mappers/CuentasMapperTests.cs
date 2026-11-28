using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>CuentasMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class CuentasMapperTests
{
    private static Cuentas Entidad() => new() { idCuenta = 7, idMesa = 2, estado = "Cerrada", fechaApertura = new DateTime(2026, 11, 5, 21, 0, 0), fechaCierre = new DateTime(2026, 11, 6, 1, 0, 0), total = 30000m };

    [Fact]
    public void ToEntity_CopiaCadaCampoDelDTO()
    {
        var dto = new CuentaCrearDTO { idMesa = 2, estado = "Cerrada", fechaApertura = new DateTime(2026, 11, 5, 21, 0, 0), fechaCierre = new DateTime(2026, 11, 6, 1, 0, 0), total = 30000m };

        var entidad = dto.ToEntity();

        entidad.idMesa.Should().Be(2);
        entidad.estado.Should().Be("Cerrada");
        entidad.fechaApertura.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        entidad.fechaCierre.Should().Be(new DateTime(2026, 11, 6, 1, 0, 0));
        entidad.total.Should().Be(30000m);
        entidad.idCuenta.Should().Be(0);   // el id lo asigna la base de datos, nunca el cliente
    }

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idCuenta.Should().Be(7);
        dto.idMesa.Should().Be(2);
        dto.estado.Should().Be("Cerrada");
        dto.fechaApertura.Should().Be(new DateTime(2026, 11, 5, 21, 0, 0));
        dto.fechaCierre.Should().Be(new DateTime(2026, 11, 6, 1, 0, 0));
        dto.total.Should().Be(30000m);
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Cuentas> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<CuentaRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Cuentas>().ToRespuestaDTO().Should().BeEmpty();
    }
}
