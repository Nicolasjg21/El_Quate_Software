using Backend.Tests.Ayudas;
using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// Cobro de una cuenta (CuentasRepository.Cobrar) contra el "bar de prueba" (ver EscenarioBar).
/// Cuenta 1: mesa 1, Abierta, pedido 1 con 2 Águila a $5.000 = $10.000 (base $9.259 + INC $741).
/// Cuentas 2 y 3: ya cobradas (comprobantes 1 y 2).
/// La base en memoria no tiene transacciones: aquí se prueban las reglas y el resultado; el bloqueo
/// por cuenta y el índice único se verifican en SQL Server con el script 006.
/// </summary>
public class CobroCuentaRepositoryTests
{
    private const int cajero = 2;

    /// <summary>Escenario con la mesa 1 "Cerrando" (cuenta enviada a caja), como queda antes de cobrar.</summary>
    private static ElQuateDePattyContext EscenarioEnCaja(string estadoMesa = "Cerrando")
    {
        var ctx = EscenarioBar.Crear();
        ctx.mesas.Single(m => m.idMesa == 1).estado = estadoMesa;
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return ctx;
    }

    [Fact]
    public async Task Cobrar_CuentaAbierta_EmiteComprobanteConBaseEInc()
    {
        using var ctx = EscenarioEnCaja();

        var r = await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        r.estado.Should().Be(EstadoCobro.Cobrada);
        r.comprobante!.idComprobante.Should().BeGreaterThan(0);
        var guardado = await ctx.comprobantes.AsNoTracking().SingleAsync(c => c.idCuenta == 1);
        guardado.total.Should().Be(10000m);
        guardado.baseGravable.Should().Be(9259m);
        guardado.impuesto.Should().Be(741m);
        guardado.tasaImpuesto.Should().Be(0.08m);
        guardado.idMetodo.Should().Be(1);
    }

    [Fact]
    public async Task Cobrar_ElTotalSaleDeLasLineasYNoDelTotalGuardadoEnLaCuenta()
    {
        // En el escenario, Cuentas.total de la cuenta 1 dice $0, pero sus líneas suman $10.000.
        using var ctx = EscenarioEnCaja();

        var r = await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        r.totalActual.Should().Be(10000m);
        (await ctx.cuentas.AsNoTracking().SingleAsync(c => c.idCuenta == 1)).total.Should().Be(10000m);
    }

    [Fact]
    public async Task Cobrar_CierraLaCuentaPagaLosPedidosYLiberaLaMesa()
    {
        using var ctx = EscenarioEnCaja();

        await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        var cuenta = await ctx.cuentas.AsNoTracking().SingleAsync(c => c.idCuenta == 1);
        cuenta.estado.Should().Be("Cerrada");
        cuenta.fechaCierre.Should().NotBeNull();
        (await ctx.pedidos.AsNoTracking().Where(p => p.idCuenta == 1).Select(p => p.estadoPedido).ToListAsync())
            .Should().OnlyContain(e => e == "Pagado");
        (await ctx.mesas.AsNoTracking().SingleAsync(m => m.idMesa == 1)).estado.Should().Be("Libre");
    }

    [Fact]
    public async Task Cobrar_LaFechaDeCierreEsLaDelComprobante()
    {
        using var ctx = EscenarioEnCaja();

        var r = await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        r.cuenta!.fechaCierre.Should().Be(r.comprobante!.fecha);
        r.comprobante.fecha.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Cobrar_EscribeLaAuditoriaConElUsuarioQueCobra()
    {
        using var ctx = EscenarioEnCaja();
        var antes = await ctx.auditorias.CountAsync();

        await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        var nuevas = await ctx.auditorias.AsNoTracking().OrderBy(a => a.idAuditoria).Skip(antes).ToListAsync();
        nuevas.Select(a => $"{a.tabla}/{a.accion}").Should().Equal("Comprobantes/INSERT", "Cuentas/UPDATE", "Mesas/UPDATE");
        nuevas.Should().OnlyContain(a => a.idUsuario == cajero && a.fecha != null);
        nuevas[0].datosNuevos.Should().Contain("\"total\":10000").And.Contain("\"impuesto\":741");
        nuevas[1].datosAnteriores.Should().Contain("\"estado\":\"Abierta\"");
        nuevas[1].datosNuevos.Should().Contain("\"estado\":\"Cerrada\"");
        nuevas[2].datosNuevos.Should().Contain("\"estado\":\"Libre\"");
    }

    [Fact]
    public async Task Cobrar_DosVeces_NoEmiteUnSegundoComprobante()
    {
        using var ctx = EscenarioEnCaja();
        var repo = new CuentasRepository(ctx);

        var primero = await repo.Cobrar(1, 1, null, cajero);
        var segundo = await repo.Cobrar(1, 2, null, cajero);

        segundo.estado.Should().Be(EstadoCobro.YaCobrada);
        segundo.comprobante!.idComprobante.Should().Be(primero.comprobante!.idComprobante);
        segundo.comprobante.idMetodo.Should().Be(1, "se devuelve el comprobante original, no se cambia el método");
        (await ctx.comprobantes.CountAsync(c => c.idCuenta == 1)).Should().Be(1);
    }

    [Fact]
    public async Task Cobrar_CuentaYaCobrada_DevuelveSuComprobanteSinEscribirNada()
    {
        using var ctx = EscenarioBar.Crear();
        var auditorias = await ctx.auditorias.CountAsync();

        var r = await new CuentasRepository(ctx).Cobrar(2, 1, null, cajero);

        r.estado.Should().Be(EstadoCobro.YaCobrada);
        r.comprobante!.idComprobante.Should().Be(1);
        (await ctx.comprobantes.CountAsync()).Should().Be(2);
        (await ctx.auditorias.CountAsync()).Should().Be(auditorias);
    }

    [Fact]
    public async Task Cobrar_CuentaInexistente_NoExiste()
    {
        using var ctx = EscenarioBar.Crear();

        (await new CuentasRepository(ctx).Cobrar(99, 1, null, cajero)).estado.Should().Be(EstadoCobro.CuentaNoExiste);
    }

    [Fact]
    public async Task Cobrar_MetodoInexistente_NoCobraNadaNiCambiaLaCuenta()
    {
        using var ctx = EscenarioEnCaja();

        var r = await new CuentasRepository(ctx).Cobrar(1, 99, null, cajero);

        r.estado.Should().Be(EstadoCobro.MetodoNoExiste);
        await SinCambios(ctx);
    }

    [Fact]
    public async Task Cobrar_TotalQueVioQuienCobraEsDistinto_NoCobraYDevuelveElActual()
    {
        using var ctx = EscenarioEnCaja();

        var r = await new CuentasRepository(ctx).Cobrar(1, 1, 5000m, cajero);

        r.estado.Should().Be(EstadoCobro.TotalDesactualizado);
        r.totalActual.Should().Be(10000m);
        await SinCambios(ctx);
    }

    [Fact]
    public async Task Cobrar_TotalQueVioQuienCobraCoincide_Cobra()
    {
        using var ctx = EscenarioEnCaja();

        (await new CuentasRepository(ctx).Cobrar(1, 1, 10000m, cajero)).estado.Should().Be(EstadoCobro.Cobrada);
    }

    [Fact]
    public async Task Cobrar_CuentaSinProductos_NoCobra()
    {
        using var ctx = EscenarioBar.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 4, idMesa = 2, estado = "Abierta", fechaApertura = DateTime.Now, total = 0m });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var r = await new CuentasRepository(ctx).Cobrar(4, 1, null, cajero);

        r.estado.Should().Be(EstadoCobro.SinProductos);
        (await ctx.comprobantes.CountAsync(c => c.idCuenta == 4)).Should().Be(0);
    }

    [Fact]
    public async Task Cobrar_CuentaCerradaSinComprobante_NoSeCobra()
    {
        using var ctx = EscenarioBar.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 4, idMesa = 2, estado = "Cerrada", fechaApertura = DateTime.Now, total = 0m });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        (await new CuentasRepository(ctx).Cobrar(4, 1, null, cajero)).estado.Should().Be(EstadoCobro.CuentaNoAbierta);
    }

    [Fact]
    public async Task Cobrar_MesaInactiva_NoSeReactiva()
    {
        using var ctx = EscenarioEnCaja("Inactiva");

        var r = await new CuentasRepository(ctx).Cobrar(1, 1, null, cajero);

        r.estado.Should().Be(EstadoCobro.Cobrada);
        (await ctx.mesas.AsNoTracking().SingleAsync(m => m.idMesa == 1)).estado.Should().Be("Inactiva");
        (await ctx.auditorias.AsNoTracking().AnyAsync(a => a.tabla == "Mesas")).Should().BeFalse();
    }

    /// <summary>Nada cambió: la cuenta 1 sigue abierta, sin comprobante, y no hay auditoría nueva.</summary>
    private static async Task SinCambios(ElQuateDePattyContext ctx)
    {
        (await ctx.comprobantes.CountAsync(c => c.idCuenta == 1)).Should().Be(0);
        (await ctx.cuentas.AsNoTracking().SingleAsync(c => c.idCuenta == 1)).estado.Should().Be("Abierta");
        (await ctx.mesas.AsNoTracking().SingleAsync(m => m.idMesa == 1)).estado.Should().Be("Cerrando");
        (await ctx.auditorias.CountAsync()).Should().Be(0);
    }
}
