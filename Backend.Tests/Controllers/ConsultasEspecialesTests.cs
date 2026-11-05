using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Compras;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>Consultas que no son CRUD: por estado, filtros, historiales y analíticas.</summary>
public class ConsultasEspecialesTests
{
    private static ControllerContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    // ---------- Mesas por estado ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMesasByEstado_EstadoVacio_Retorna400(string estado)
    {
        var repo = new Mock<IMesasRepository>();
        var sut = new MesasController(repo.Object, NullLogger<MesasController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetMesasByEstado(estado)).Should().BeOfType<BadRequestObjectResult>();
        repo.Verify(r => r.GetMesasByEstado(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMesasByEstado_HayMesasLibres_Retorna200()
    {
        var repo = new Mock<IMesasRepository>();
        repo.Setup(r => r.GetMesasByEstado("Libre")).ReturnsAsync(new List<Mesas> { new() { idMesa = 1, numeroMesa = 1, estado = "Libre" } });
        var sut = new MesasController(repo.Object, NullLogger<MesasController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetMesasByEstado("Libre")).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMesasByEstado_NingunaEnEseEstado_Retorna404()
    {
        var repo = new Mock<IMesasRepository>();
        repo.Setup(r => r.GetMesasByEstado("Ocupada")).ReturnsAsync(new List<Mesas>());
        var sut = new MesasController(repo.Object, NullLogger<MesasController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetMesasByEstado("Ocupada")).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMesasByEstado_RepositorioFalla_Retorna500()
    {
        var repo = new Mock<IMesasRepository>();
        repo.Setup(r => r.GetMesasByEstado(It.IsAny<string>())).ThrowsAsync(new Exception("Error"));
        var sut = new MesasController(repo.Object, NullLogger<MesasController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetMesasByEstado("Libre")).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task FiltrarMesas_ConFiltro_Retorna200YUsaElFiltro()
    {
        var repo = new Mock<IMesasRepository>();
        var filtro = new MesaFiltroDTO { estado = "Libre" };
        repo.Setup(r => r.FiltrarMesas(filtro)).ReturnsAsync(new List<Mesas>());
        var sut = new MesasController(repo.Object, NullLogger<MesasController>.Instance) { ControllerContext = Contexto() };

        (await sut.FiltrarMesas(filtro)).Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.FiltrarMesas(filtro), Times.Once);
    }

    // ---------- Cuentas por estado ----------

    [Fact]
    public async Task GetCuentas_ConEstado_PasaElFiltroAlRepositorio()
    {
        var repo = new Mock<ICuentasRepository>();
        repo.Setup(r => r.GetCuentas("Abierta")).ReturnsAsync(new List<Cuentas> { new() { idCuenta = 1, estado = "Abierta" } });
        var sut = new CuentasController(repo.Object, NullLogger<CuentasController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetCuentas("Abierta")).Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.GetCuentas("Abierta"), Times.Once);
    }

    // ---------- Detalle de pedidos por cuenta ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task GetDetallePedidos_IdCuentaNoPositivo_Retorna400(int idCuenta)
    {
        var repo = new Mock<IDetallePedidosRepository>();
        var sut = new DetallePedidosController(repo.Object, NullLogger<DetallePedidosController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetDetallePedidos(idCuenta)).Should().BeOfType<BadRequestObjectResult>();
        repo.Verify(r => r.GetDetallePedidos(It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task GetDetallePedidos_DeUnaCuenta_PasaElFiltroAlRepositorio()
    {
        var repo = new Mock<IDetallePedidosRepository>();
        repo.Setup(r => r.GetDetallePedidos(4)).ReturnsAsync(new List<DetallePedidos> { new() { idDetalle = 1, cantidad = 2 } });
        var sut = new DetallePedidosController(repo.Object, NullLogger<DetallePedidosController>.Instance) { ControllerContext = Contexto() };

        (await sut.GetDetallePedidos(4)).Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.GetDetallePedidos(4), Times.Once);
    }

    // ---------- Pedidos: filtro e historial ----------

    [Fact]
    public async Task FiltrarPedidos_ConFiltro_Retorna200()
    {
        var repo = new Mock<IPedidosRepository>();
        var filtro = new PedidoFiltroDTO { numeroMesa = 3 };
        repo.Setup(r => r.FiltrarPedidos(filtro)).ReturnsAsync(new List<PedidoFiltradoRespuestaDTO>());
        var sut = new PedidosController(repo.Object, NullLogger<PedidosController>.Instance) { ControllerContext = Contexto() };

        (await sut.FiltrarPedidos(filtro)).Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.FiltrarPedidos(filtro), Times.Once);
    }

    [Fact]
    public async Task FiltrarHistorialPedidos_ConPeriodo_Retorna200()
    {
        var repo = new Mock<IPedidosRepository>();
        var periodo = new PeriodoFiltroDTO { fechaDesde = new DateTime(2026, 11, 1), fechaHasta = new DateTime(2026, 11, 5) };
        repo.Setup(r => r.FiltrarHistorialPedidos(periodo)).ReturnsAsync(new List<Pedidos>());
        var sut = new PedidosController(repo.Object, NullLogger<PedidosController>.Instance) { ControllerContext = Contexto() };

        (await sut.FiltrarHistorialPedidos(periodo)).Should().BeOfType<OkObjectResult>();
    }

    // ---------- Compras: filtro e historial ----------

    [Fact]
    public async Task FiltrarCompras_ConFiltro_Retorna200()
    {
        var repo = new Mock<IComprasRepository>();
        var filtro = new CompraFiltroDTO { idProveedor = 1 };
        repo.Setup(r => r.FiltrarCompras(filtro)).ReturnsAsync(new List<Compras>());
        var sut = new ComprasController(repo.Object, NullLogger<ComprasController>.Instance) { ControllerContext = Contexto() };

        (await sut.FiltrarCompras(filtro)).Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.FiltrarCompras(filtro), Times.Once);
    }

    [Fact]
    public async Task FiltrarHistorialCompras_ConPeriodo_Retorna200()
    {
        var repo = new Mock<IComprasRepository>();
        var periodo = new PeriodoFiltroDTO();
        repo.Setup(r => r.FiltrarHistorialCompras(periodo)).ReturnsAsync(new List<Compras>());
        var sut = new ComprasController(repo.Object, NullLogger<ComprasController>.Instance) { ControllerContext = Contexto() };

        (await sut.FiltrarHistorialCompras(periodo)).Should().BeOfType<OkObjectResult>();
    }

    // ---------- Analíticas ----------

    [Fact]
    public async Task ObtenerAnaliticasVentas_ConPeriodo_Retorna200ConLosTotales()
    {
        var repo = new Mock<IAnaliticasRepository>();
        var periodo = new PeriodoFiltroDTO { fechaDesde = new DateTime(2026, 11, 1) };
        repo.Setup(r => r.ObtenerAnaliticasVentas(periodo))
            .ReturnsAsync(new VentaAnaliticaDTO { totalVentas = 12, totalIngresos = 360000m, promedioVenta = 30000m });
        var sut = new AnaliticasController(repo.Object) { ControllerContext = Contexto() };

        var resultado = await sut.ObtenerAnaliticasVentas(periodo);

        resultado.Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.ObtenerAnaliticasVentas(periodo), Times.Once);
    }
}
