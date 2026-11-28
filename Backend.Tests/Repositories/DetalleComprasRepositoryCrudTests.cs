using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// DetalleComprasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class DetalleComprasRepositoryCrudTests
{
    [Fact]
    public async Task GetDetalleCompras_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detalleCompras.AddRange(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 }, new DetalleCompras { idDetalleCompra = 2, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });
        await ctx.SaveChangesAsync();

        var lista = await new DetalleComprasRepository(ctx).GetDetalleCompras();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDetalleCompras_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new DetalleComprasRepository(ctx).GetDetalleCompras();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDetalleComprasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detalleCompras.Add(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });
        await ctx.SaveChangesAsync();

        var encontrado = await new DetalleComprasRepository(ctx).GetDetalleComprasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.cantidad.Should().Be(24);
    }

    [Fact]
    public async Task GetDetalleComprasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new DetalleComprasRepository(ctx).GetDetalleComprasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostDetalleCompras_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new DetalleComprasRepository(ctx).PostDetalleCompras(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        guardado.Should().BeTrue();
        (await ctx.detalleCompras.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutDetalleCompras_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detalleCompras.Add(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });
        await ctx.SaveChangesAsync();
        var repo = new DetalleComprasRepository(ctx);
        var existente = await repo.GetDetalleComprasById(1);
        existente!.cantidad = 48;

        var actualizado = await repo.PutDetalleCompras(existente);

        actualizado.Should().BeTrue();
        (await ctx.detalleCompras.AsNoTracking().SingleAsync()).cantidad.Should().Be(48);
    }

    [Fact]
    public async Task PutDetalleCompras_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detalleCompras.Add(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });
        await ctx.SaveChangesAsync();
        var repo = new DetalleComprasRepository(ctx);
        var existente = await repo.GetDetalleComprasById(1);

        (await repo.PutDetalleCompras(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDetalleCompras_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.detalleCompras.Add(new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });
        await ctx.SaveChangesAsync();
        var repo = new DetalleComprasRepository(ctx);
        var existente = await repo.GetDetalleComprasById(1);

        var eliminado = await repo.DeleteDetalleCompras(existente!);

        eliminado.Should().BeTrue();
        (await ctx.detalleCompras.CountAsync()).Should().Be(0);
    }
}
