using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>Costo promedio ponderado móvil: solo una ENTRADA con costo de compra lo cambia.</summary>
public class CostoPromedioTests
{
    [Fact]
    public void PrimeraCompra_ElPromedioEsElCostoDeCompra()
    {
        CostoPromedio.Calcular(0, 0m, "ENTRADA", 24, 3000m).Should().Be((3000m, 3000m));
    }

    [Fact]
    public void SegundaCompra_PonderaPorLoQueHayEnBodega()
    {
        // 100 unidades a $3.000 en bodega + 1 a $5.000 = 305.000 / 101 = 3.019,80
        CostoPromedio.Calcular(100, 3000m, "ENTRADA", 1, 5000m).Should().Be((5000m, 3019.80m));
    }

    [Fact]
    public void CompraConPocoStock_PesaMasElPrecioNuevo()
    {
        // Quedan 4 a $3.000 (se vendió casi todo) y entran 20 a $3.500: (12.000 + 70.000) / 24 = 3.416,67
        CostoPromedio.Calcular(4, 3000m, "ENTRADA", 20, 3500m).Should().Be((3500m, 3416.67m));
    }

    [Fact]
    public void CompraConBodegaVacia_ElPromedioEsElNuevoCosto()
    {
        CostoPromedio.Calcular(0, 3000m, "ENTRADA", 10, 3600m).Should().Be((3600m, 3600m));
    }

    [Theory]
    [InlineData("SALIDA")]
    [InlineData("AJUSTE")]
    public void VentaOAjuste_SeValoranAlPromedioYNoLoCambian(string tipo)
    {
        CostoPromedio.Calcular(24, 3125.50m, tipo, 3, null).Should().Be((3125.50m, 3125.50m));
    }

    [Fact]
    public void EntradaSinCosto_DevolucionABodega_NoCambiaElPromedio()
    {
        CostoPromedio.Calcular(10, 3121m, "ENTRADA", 2, null).Should().Be((3121m, 3121m));
    }

    [Fact]
    public void CostoDeCompraEnUnaSalida_SeIgnora()
    {
        CostoPromedio.Calcular(10, 3121m, "SALIDA", 2, 9999m).Should().Be((3121m, 3121m));
    }

    [Fact]
    public void Redondea_ACentavos_HaciaArribaEnElPuntoMedio()
    {
        // (1 × 1,00 + 1 × 1,01) / 2 = 1,005 → 1,01
        CostoPromedio.Calcular(1, 1.00m, "ENTRADA", 1, 1.01m).costoPromedio.Should().Be(1.01m);
    }

    [Fact]
    public void StockAnteriorNegativo_SeTrataComoBodegaVacia()
    {
        CostoPromedio.Calcular(-3, 2000m, "ENTRADA", 5, 2500m).Should().Be((2500m, 2500m));
    }
}
