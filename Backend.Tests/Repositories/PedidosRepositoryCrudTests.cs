using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// PedidosRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class PedidosRepositoryCrudTests
{
    [Fact]
    public async Task GetPedidos_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.pedidos.AddRange(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" }, new Pedidos { idPedido = 2, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Entregado" });
        await ctx.SaveChangesAsync();

        var lista = await new PedidosRepository(ctx).GetPedidos();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPedidos_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new PedidosRepository(ctx).GetPedidos();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPedidosById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.pedidos.Add(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" });
        await ctx.SaveChangesAsync();

        var encontrado = await new PedidosRepository(ctx).GetPedidosById(1);

        encontrado.Should().NotBeNull();
        encontrado!.estadoPedido.Should().Be("Pendiente");
    }

    [Fact]
    public async Task GetPedidosById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new PedidosRepository(ctx).GetPedidosById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostPedidos_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new PedidosRepository(ctx).PostPedidos(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" });

        guardado.Should().BeTrue();
        (await ctx.pedidos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutPedidos_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.pedidos.Add(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" });
        await ctx.SaveChangesAsync();
        var repo = new PedidosRepository(ctx);
        var existente = await repo.GetPedidosById(1);
        existente!.estadoPedido = "Entregado";

        var actualizado = await repo.PutPedidos(existente);

        actualizado.Should().BeTrue();
        (await ctx.pedidos.AsNoTracking().SingleAsync()).estadoPedido.Should().Be("Entregado");
    }

    [Fact]
    public async Task PutPedidos_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.pedidos.Add(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" });
        await ctx.SaveChangesAsync();
        var repo = new PedidosRepository(ctx);
        var existente = await repo.GetPedidosById(1);

        (await repo.PutPedidos(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeletePedidos_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.pedidos.Add(new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), estadoPedido = "Pendiente" });
        await ctx.SaveChangesAsync();
        var repo = new PedidosRepository(ctx);
        var existente = await repo.GetPedidosById(1);

        var eliminado = await repo.DeletePedidos(existente!);

        eliminado.Should().BeTrue();
        (await ctx.pedidos.CountAsync()).Should().Be(0);
    }
}
