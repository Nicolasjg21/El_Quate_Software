using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// DetallePedidosRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class DetallePedidosRepositoryCrudTests
{
    [Fact]
    public async Task GetDetallePedidos_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detallePedidos.AddRange(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 }, new DetallePedidos { idDetalle = 2, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });
        await ctx.SaveChangesAsync();

        var lista = await new DetallePedidosRepository(ctx).GetDetallePedidos();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDetallePedidos_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new DetallePedidosRepository(ctx).GetDetallePedidos();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetallePedidosById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detallePedidos.Add(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });
        await ctx.SaveChangesAsync();

        var encontrado = await new DetallePedidosRepository(ctx).GetDetallePedidosById(1);

        encontrado.Should().NotBeNull();
        encontrado!.cantidad.Should().Be(2);
    }

    [Fact]
    public async Task GetDetallePedidosById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new DetallePedidosRepository(ctx).GetDetallePedidosById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostDetallePedidos_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new DetallePedidosRepository(ctx).PostDetallePedidos(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        guardado.Should().BeTrue();
        (await ctx.detallePedidos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutDetallePedidos_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detallePedidos.Add(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });
        await ctx.SaveChangesAsync();
        var repo = new DetallePedidosRepository(ctx);
        var existente = await repo.GetDetallePedidosById(1);
        existente!.cantidad = 3;

        var actualizado = await repo.PutDetallePedidos(existente);

        actualizado.Should().BeTrue();
        (await ctx.detallePedidos.AsNoTracking().SingleAsync()).cantidad.Should().Be(3);
    }

    [Fact]
    public async Task PutDetallePedidos_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detallePedidos.Add(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });
        await ctx.SaveChangesAsync();
        var repo = new DetallePedidosRepository(ctx);
        var existente = await repo.GetDetallePedidosById(1);

        (await repo.PutDetallePedidos(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDetallePedidos_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detallePedidos.Add(new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });
        await ctx.SaveChangesAsync();
        var repo = new DetallePedidosRepository(ctx);
        var existente = await repo.GetDetallePedidosById(1);

        var eliminado = await repo.DeleteDetallePedidos(existente!);

        eliminado.Should().BeTrue();
        (await ctx.detallePedidos.CountAsync()).Should().Be(0);
    }
}
