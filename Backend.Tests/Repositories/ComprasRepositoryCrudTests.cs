using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// ComprasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class ComprasRepositoryCrudTests
{
    [Fact]
    public async Task GetCompras_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.compras.AddRange(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m }, new Compras { idCompra = 2, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 150000m });
        await ctx.SaveChangesAsync();

        var lista = await new ComprasRepository(ctx).GetCompras();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCompras_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new ComprasRepository(ctx).GetCompras();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComprasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.compras.Add(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m });
        await ctx.SaveChangesAsync();

        var encontrado = await new ComprasRepository(ctx).GetComprasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.total.Should().Be(120000m);
    }

    [Fact]
    public async Task GetComprasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new ComprasRepository(ctx).GetComprasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostCompras_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new ComprasRepository(ctx).PostCompras(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m });

        guardado.Should().BeTrue();
        (await ctx.compras.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutCompras_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.compras.Add(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprasRepository(ctx);
        var existente = await repo.GetComprasById(1);
        existente!.total = 150000m;

        var actualizado = await repo.PutCompras(existente);

        actualizado.Should().BeTrue();
        (await ctx.compras.AsNoTracking().SingleAsync()).total.Should().Be(150000m);
    }

    [Fact]
    public async Task PutCompras_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.compras.Add(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprasRepository(ctx);
        var existente = await repo.GetComprasById(1);

        (await repo.PutCompras(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCompras_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.compras.Add(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 120000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprasRepository(ctx);
        var existente = await repo.GetComprasById(1);

        var eliminado = await repo.DeleteCompras(existente!);

        eliminado.Should().BeTrue();
        (await ctx.compras.CountAsync()).Should().Be(0);
    }
}
