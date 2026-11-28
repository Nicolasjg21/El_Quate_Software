using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>INC 8 % incluido: el total se separa en base (total / 1,08 en pesos) e impuesto (total − base).</summary>
public class ImpuestoConsumoTests
{
    [Theory]
    [InlineData(5000, 4630, 370)]       // 5.000 / 1,08 = 4.629,63
    [InlineData(10000, 9259, 741)]      // 9.259,26
    [InlineData(30000, 27778, 2222)]    // 27.777,78
    [InlineData(90000, 83333, 6667)]    // 83.333,33
    [InlineData(3500, 3241, 259)]       // 3.240,74
    [InlineData(54, 50, 4)]             // exacto
    [InlineData(0, 0, 0)]
    public void Desglosar_SeparaBaseEImpuesto(int total, int baseEsperada, int impuestoEsperado)
    {
        ImpuestoConsumo.Desglosar(total).Should().Be(((decimal)baseEsperada, (decimal)impuestoEsperado));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(12345)]
    [InlineData(287650)]
    [InlineData(1999999)]
    public void Desglosar_BaseMasImpuestoSiempreEsElTotal(int total)
    {
        var (baseGravable, impuesto) = ImpuestoConsumo.Desglosar(total);

        (baseGravable + impuesto).Should().Be(total);
        impuesto.Should().BeGreaterThanOrEqualTo(0);
        baseGravable.Should().Be(Math.Round(baseGravable, 0));
    }

    [Fact]
    public void Tasa_EsOchoPorCiento()
    {
        ImpuestoConsumo.Tasa.Should().Be(0.08m);
    }

    [Fact]
    public void ComprobanteDesdeDto_CalculaBaseEImpuestoEnElServidor()
    {
        var entidad = new ComprobanteCrearDTO { idCuenta = 1, idMetodo = 1, total = 5000m, fecha = new DateTime(2026, 11, 28) }.ToEntity();

        entidad.baseGravable.Should().Be(4630m);
        entidad.impuesto.Should().Be(370m);
        entidad.tasaImpuesto.Should().Be(0.08m);
    }

    [Fact]
    public void AplicarImpuesto_RecalculaSiCambiaElTotal()
    {
        var comprobante = new Comprobantes { total = 5000m };
        comprobante.AplicarImpuesto();

        comprobante.total = 10000m;
        comprobante.AplicarImpuesto();

        comprobante.baseGravable.Should().Be(9259m);
        comprobante.impuesto.Should().Be(741m);
    }

    [Fact]
    public void RespuestaDelComprobante_IncluyeBaseImpuestoYTasa()
    {
        var dto = new Comprobantes { idComprobante = 7, idCuenta = 3, total = 5000m, baseGravable = 4630m, impuesto = 370m, tasaImpuesto = 0.08m }
            .ToRespuestaDTO();

        dto.baseGravable.Should().Be(4630m);
        dto.impuesto.Should().Be(370m);
        dto.tasaImpuesto.Should().Be(0.08m);
    }
}
