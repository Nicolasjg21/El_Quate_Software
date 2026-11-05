using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// MetodosPagoRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class MetodosPagoRepositoryCrudTests
{
    [Fact]
    public async Task GetMetodosPago_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.metodosPago.AddRange(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" }, new MetodosPago { idMetodo = 2, nombreMetodo = "Nequi" });
        await ctx.SaveChangesAsync();

        var lista = await new MetodosPagoRepository(ctx).GetMetodosPago();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMetodosPago_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new MetodosPagoRepository(ctx).GetMetodosPago();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetodosPagoById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.metodosPago.Add(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        await ctx.SaveChangesAsync();

        var encontrado = await new MetodosPagoRepository(ctx).GetMetodosPagoById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreMetodo.Should().Be("Efectivo");
    }

    [Fact]
    public async Task GetMetodosPagoById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new MetodosPagoRepository(ctx).GetMetodosPagoById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostMetodosPago_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new MetodosPagoRepository(ctx).PostMetodosPago(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });

        guardado.Should().BeTrue();
        (await ctx.metodosPago.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutMetodosPago_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.metodosPago.Add(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        await ctx.SaveChangesAsync();
        var repo = new MetodosPagoRepository(ctx);
        var existente = await repo.GetMetodosPagoById(1);
        existente!.nombreMetodo = "Nequi";

        var actualizado = await repo.PutMetodosPago(existente);

        actualizado.Should().BeTrue();
        (await ctx.metodosPago.AsNoTracking().SingleAsync()).nombreMetodo.Should().Be("Nequi");
    }

    [Fact]
    public async Task PutMetodosPago_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.metodosPago.Add(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        await ctx.SaveChangesAsync();
        var repo = new MetodosPagoRepository(ctx);
        var existente = await repo.GetMetodosPagoById(1);

        (await repo.PutMetodosPago(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMetodosPago_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.metodosPago.Add(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" });
        await ctx.SaveChangesAsync();
        var repo = new MetodosPagoRepository(ctx);
        var existente = await repo.GetMetodosPagoById(1);

        var eliminado = await repo.DeleteMetodosPago(existente!);

        eliminado.Should().BeTrue();
        (await ctx.metodosPago.CountAsync()).Should().Be(0);
    }
}
