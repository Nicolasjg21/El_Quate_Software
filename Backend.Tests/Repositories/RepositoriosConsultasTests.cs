using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Compras;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// Consultas y filtros de los repositorios contra el "bar de prueba" (ver EscenarioBar).
/// Cada resultado esperado se puede calcular a mano con los datos del escenario.
/// </summary>
public class RepositoriosConsultasTests
{
    // ---------- Cuentas ----------

    [Fact]
    public async Task GetCuentas_SinEstado_DevuelveTodas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new CuentasRepository(ctx).GetCuentas()).Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCuentas_EstadoAbierta_DevuelveSoloLasAbiertas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new CuentasRepository(ctx).GetCuentas("Abierta")).Select(c => c.idCuenta).Should().Equal(1);
    }

    [Fact]
    public async Task GetCuentas_EstadoConEspacios_SeRecortaAntesDeFiltrar()
    {
        using var ctx = EscenarioBar.Crear();
        (await new CuentasRepository(ctx).GetCuentas("  Cerrada ")).Should().HaveCount(2);
    }

    // ---------- Detalle de pedidos ----------

    [Fact]
    public async Task GetDetallePedidos_DeUnaCuenta_DevuelveSoloSusLineas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new DetallePedidosRepository(ctx).GetDetallePedidos(3)).Select(d => d.idDetalle).Should().BeEquivalentTo(new[] { 3, 4 });
    }

    [Fact]
    public async Task GetDetallePedidos_SinCuenta_DevuelveTodas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new DetallePedidosRepository(ctx).GetDetallePedidos()).Should().HaveCount(4);
    }

    // ---------- Mesas ----------

    [Fact]
    public async Task GetMesasByEstado_Ocupada_DevuelveLasDos()
    {
        using var ctx = EscenarioBar.Crear();
        (await new MesasRepository(ctx).GetMesasByEstado("Ocupada")).Select(m => m.numeroMesa).Should().BeEquivalentTo(new[] { 2, 3 });
    }

    [Fact]
    public async Task FiltrarMesas_SinEstado_DevuelveTodas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new MesasRepository(ctx).FiltrarMesas(new MesaFiltroDTO())).Should().HaveCount(3);
    }

    [Fact]
    public async Task FiltrarMesas_Libre_DevuelveLaMesa1()
    {
        using var ctx = EscenarioBar.Crear();
        (await new MesasRepository(ctx).FiltrarMesas(new MesaFiltroDTO { estado = "Libre" })).Select(m => m.numeroMesa).Should().Equal(1);
    }

    // ---------- Proveedores ----------

    [Fact]
    public async Task FiltrarProveedores_SinFiltros_DevuelveTodos()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProveedoresRepository(ctx).FiltrarProveedores(new ProveedorFiltroDTO())).Should().HaveCount(2);
    }

    [Theory]
    [InlineData("Bav", null, null, "Bavaria")]
    [InlineData(null, "310", null, "Dislicores")]
    [InlineData(null, null, "Calle", "Dislicores")]
    public async Task FiltrarProveedores_PorTextoParcial_EncuentraElCorrecto(string? nombre, string? telefono, string? direccion, string esperado)
    {
        using var ctx = EscenarioBar.Crear();
        var filtro = new ProveedorFiltroDTO { nombreProveedor = nombre, telefono = telefono, direccion = direccion };

        (await new ProveedoresRepository(ctx).FiltrarProveedores(filtro)).Select(p => p.nombreProveedor).Should().Equal(esperado);
    }

    // ---------- Compras ----------

    [Fact]
    public async Task FiltrarCompras_DesdeUnaFecha_DevuelveLasPosteriores()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ComprasRepository(ctx).FiltrarCompras(new CompraFiltroDTO { fechaDesde = new DateTime(2026, 11, 2) }))
            .Select(c => c.idCompra).Should().Equal(2);
    }

    [Fact]
    public async Task FiltrarCompras_HastaUnaFecha_DevuelveLasAnteriores()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ComprasRepository(ctx).FiltrarCompras(new CompraFiltroDTO { fechaHasta = new DateTime(2026, 11, 2) }))
            .Select(c => c.idCompra).Should().Equal(1);
    }

    [Fact]
    public async Task FiltrarCompras_PorProveedor_DevuelveSusCompras()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ComprasRepository(ctx).FiltrarCompras(new CompraFiltroDTO { idProveedor = 1 })).Select(c => c.idCompra).Should().Equal(1);
    }

    [Theory]
    [InlineData(10, null, 1)]      // compra 1 tiene 36 artículos
    [InlineData(null, 10, 2)]      // compra 2 tiene 5 artículos
    public async Task FiltrarCompras_PorCantidadDeArticulos_SumaLosDetalles(int? minimo, int? maximo, int esperada)
    {
        using var ctx = EscenarioBar.Crear();
        var filtro = new CompraFiltroDTO { totalArticulosMinimo = minimo, totalArticulosMaximo = maximo };

        (await new ComprasRepository(ctx).FiltrarCompras(filtro)).Select(c => c.idCompra).Should().Equal(esperada);
    }

    [Theory]
    [InlineData(150000, null, 2)]  // compra 2 cuesta $200.000
    [InlineData(null, 150000, 1)]  // compra 1 cuesta $120.000
    public async Task FiltrarCompras_PorCostoTotal_SumaCantidadPorPrecio(int? minimo, int? maximo, int esperada)
    {
        using var ctx = EscenarioBar.Crear();
        var filtro = new CompraFiltroDTO { costoTotalMinimo = minimo, costoTotalMaximo = maximo };

        (await new ComprasRepository(ctx).FiltrarCompras(filtro)).Select(c => c.idCompra).Should().Equal(esperada);
    }

    [Fact]
    public async Task FiltrarHistorialCompras_SinPeriodo_OrdenaDeLaMasRecienteALaMasAntigua()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ComprasRepository(ctx).FiltrarHistorialCompras(new PeriodoFiltroDTO())).Select(c => c.idCompra).Should().Equal(2, 1);
    }

    // ---------- Pedidos ----------

    [Fact]
    public async Task FiltrarHistorialPedidos_DesdeEl4_DevuelveLosDelDia5OrdenadosDescendente()
    {
        using var ctx = EscenarioBar.Crear();
        var periodo = new PeriodoFiltroDTO { fechaDesde = new DateTime(2026, 11, 4) };

        (await new PedidosRepository(ctx).FiltrarHistorialPedidos(periodo)).Select(p => p.idPedido).Should().Equal(1, 3);
    }

    [Fact]
    public async Task FiltrarHistorialPedidos_HastaEl4_DevuelveElDelDia3()
    {
        using var ctx = EscenarioBar.Crear();
        var periodo = new PeriodoFiltroDTO { fechaHasta = new DateTime(2026, 11, 4) };

        (await new PedidosRepository(ctx).FiltrarHistorialPedidos(periodo)).Select(p => p.idPedido).Should().Equal(2);
    }

    [Fact]
    public async Task FiltrarPedidos_PorNumeroDeMesa_DevuelveElPedidoConSuMesaYUsuario()
    {
        using var ctx = EscenarioBar.Crear();

        var lista = await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { numeroMesa = 3 });

        lista.Should().ContainSingle();
        lista[0].idPedido.Should().Be(2);
        lista[0].cuenta!.mesa!.numeroMesa.Should().Be(3);
        lista[0].usuario!.email.Should().Be("mesero@elcuate.com");
        lista[0].cuenta!.comprobantes.Should().ContainSingle(c => c.metodoPago!.nombreMetodo == "Efectivo");
    }

    [Fact]
    public async Task FiltrarPedidos_PorUsuario_DevuelveLosDelMesero()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { idUsuario = 2 })).Select(p => p.idPedido).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task FiltrarPedidos_PorFecha_DevuelveLosDeEseDiaCompleto()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { fecha = EscenarioBar.Dia5 })).Select(p => p.idPedido).Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public async Task FiltrarPedidos_DesdeLas20_DevuelveSoloElDeLas21()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { horaInicio = new TimeSpan(20, 0, 0) })).Select(p => p.idPedido).Should().Equal(1);
    }

    [Fact]
    public async Task FiltrarPedidos_HastaLas16_DevuelveElDeLas15()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { horaFin = new TimeSpan(16, 0, 0) })).Select(p => p.idPedido).Should().Equal(3);
    }

    [Fact]
    public async Task FiltrarPedidos_TotalMinimo_DevuelveLaCuentaGrande()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { totalMinimo = 50000m })).Select(p => p.idPedido).Should().Equal(3);
    }

    [Fact]
    public async Task FiltrarPedidos_TotalMaximo_DevuelveLasCuentasPequenas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { totalMaximo = 40000m })).Select(p => p.idPedido).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task FiltrarPedidos_PorMetodoDePago_DevuelveLosPagadosConNequi()
    {
        using var ctx = EscenarioBar.Crear();
        (await new PedidosRepository(ctx).FiltrarPedidos(new PedidoFiltroDTO { idMetodoPago = 2 })).Select(p => p.idPedido).Should().Equal(3);
    }

    // ---------- Productos ----------

    [Fact]
    public async Task FiltrarProductos_SinFiltros_CalculaStockCostoYProveedor()
    {
        using var ctx = EscenarioBar.Crear();

        var lista = await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO());

        lista.Should().HaveCount(3);
        var aguila = lista.Single(p => p.idProducto == 1);
        aguila.stockActual.Should().Be(16);              // último movimiento del kardex
        aguila.costoPromedio.Should().Be(3000m);         // promedio de precios de compra
        aguila.proveedor.Should().Be("Bavaria");
        aguila.categoria.Should().Be("Cervezas");
        aguila.stockBajo.Should().BeFalse();             // 16 > mínimo 10
        lista.Single(p => p.idProducto == 2).stockBajo.Should().BeTrue();   // 2 <= mínimo 2 (valor límite)
    }

    [Fact]
    public async Task FiltrarProductos_PorNombre_EncuentraLasCervezas()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO { nombre = "Cerveza" })).Select(p => p.idProducto).Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public async Task FiltrarProductos_PorCategoria_DevuelveLosLicores()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO { idCategoria = 2 })).Select(p => p.idProducto).Should().Equal(2);
    }

    [Theory]
    [InlineData(6000, null, new[] { 2, 3 })]
    [InlineData(null, 5000, new[] { 1 })]
    public async Task FiltrarProductos_PorPrecioDeVenta_RespetaLosLimites(int? minimo, int? maximo, int[] esperados)
    {
        using var ctx = EscenarioBar.Crear();
        var filtro = new ProductoFiltroDTO { precioVentaMinimo = minimo, precioVentaMaximo = maximo };

        (await new ProductosRepository(ctx).FiltrarProductos(filtro)).Select(p => p.idProducto).Should().BeEquivalentTo(esperados);
    }

    [Theory]
    [InlineData(10000, null, new[] { 2 })]
    [InlineData(null, 5000, new[] { 1, 3 })]
    public async Task FiltrarProductos_PorCostoPromedio_RespetaLosLimites(int? minimo, int? maximo, int[] esperados)
    {
        using var ctx = EscenarioBar.Crear();
        var filtro = new ProductoFiltroDTO { costoMinimo = minimo, costoMaximo = maximo };

        (await new ProductosRepository(ctx).FiltrarProductos(filtro)).Select(p => p.idProducto).Should().BeEquivalentTo(esperados);
    }

    [Fact]
    public async Task FiltrarProductos_PorProveedor_DevuelveLoQueLeCompramos()
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO { idProveedor = 2 })).Select(p => p.idProducto).Should().Equal(2);
    }

    [Theory]
    [InlineData(true, new[] { 3 })]       // Cerveza Club: nunca tuvo movimientos → stock 0
    [InlineData(false, new[] { 1, 2 })]
    public async Task FiltrarProductos_SinStock_UsaElUltimoMovimientoDelKardex(bool sinStock, int[] esperados)
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO { sinStock = sinStock }))
            .Select(p => p.idProducto).Should().BeEquivalentTo(esperados);
    }

    [Theory]
    [InlineData(true, new[] { 2, 3 })]    // stock <= cantidad mínima
    [InlineData(false, new[] { 1 })]
    public async Task FiltrarProductos_StockBajo_ComparaContraLaCantidadMinima(bool stockBajo, int[] esperados)
    {
        using var ctx = EscenarioBar.Crear();
        (await new ProductosRepository(ctx).FiltrarProductos(new ProductoFiltroDTO { stockBajo = stockBajo }))
            .Select(p => p.idProducto).Should().BeEquivalentTo(esperados);
    }

    // ---------- Analíticas ----------

    [Fact]
    public async Task ObtenerAnaliticasVentas_SinPeriodo_SumaTodasLasVentas()
    {
        using var ctx = EscenarioBar.Crear();

        var a = await new AnaliticasRepository(ctx).ObtenerAnaliticasVentas(new PeriodoFiltroDTO());

        a.totalVentas.Should().Be(2);
        a.totalIngresos.Should().Be(120000m);
        a.promedioVenta.Should().Be(60000m);
        a.totalProductosVendidos.Should().Be(12);   // 6 Águila (cuenta 2) + 1 Aguardiente + 5 Club (cuenta 3)
    }

    [Fact]
    public async Task ObtenerAnaliticasVentas_DesdeEl4_SoloCuentaLaVentaDelDia5()
    {
        using var ctx = EscenarioBar.Crear();

        var a = await new AnaliticasRepository(ctx).ObtenerAnaliticasVentas(new PeriodoFiltroDTO { fechaDesde = new DateTime(2026, 11, 4) });

        a.totalVentas.Should().Be(1);
        a.totalIngresos.Should().Be(90000m);
        a.totalProductosVendidos.Should().Be(6);
    }

    [Fact]
    public async Task ObtenerAnaliticasVentas_PeriodoSinVentas_DevuelveCerosSinDividirPorCero()
    {
        using var ctx = EscenarioBar.Crear();

        var a = await new AnaliticasRepository(ctx).ObtenerAnaliticasVentas(new PeriodoFiltroDTO { fechaHasta = new DateTime(2025, 1, 1) });

        a.totalVentas.Should().Be(0);
        a.totalIngresos.Should().Be(0m);
        a.promedioVenta.Should().Be(0m);
        a.totalProductosVendidos.Should().Be(0);
    }
}
