using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// KardexRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class KardexRepositoryCrudTests
{
    [Fact]
    public async Task GetKardex_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.kardex.AddRange(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" }, new Kardex { idMovimiento = 2, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Corrección" });
        await ctx.SaveChangesAsync();

        var lista = await new KardexRepository(ctx).GetKardex();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetKardex_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new KardexRepository(ctx).GetKardex();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKardexById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.kardex.Add(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" });
        await ctx.SaveChangesAsync();

        var encontrado = await new KardexRepository(ctx).GetKardexById(1);

        encontrado.Should().NotBeNull();
        encontrado!.motivo.Should().Be("Compra");
    }

    [Fact]
    public async Task GetKardexById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new KardexRepository(ctx).GetKardexById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostKardex_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new KardexRepository(ctx).PostKardex(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" });

        guardado.Should().BeTrue();
        (await ctx.kardex.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutKardex_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.kardex.Add(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" });
        await ctx.SaveChangesAsync();
        var repo = new KardexRepository(ctx);
        var existente = await repo.GetKardexById(1);
        existente!.motivo = "Corrección";

        var actualizado = await repo.PutKardex(existente);

        actualizado.Should().BeTrue();
        (await ctx.kardex.AsNoTracking().SingleAsync()).motivo.Should().Be("Corrección");
    }

    [Fact]
    public async Task PutKardex_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.kardex.Add(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" });
        await ctx.SaveChangesAsync();
        var repo = new KardexRepository(ctx);
        var existente = await repo.GetKardexById(1);

        (await repo.PutKardex(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteKardex_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.kardex.Add(new Kardex { idMovimiento = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, stockAnterior = 0, stockNuevo = 10, idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), motivo = "Compra" });
        await ctx.SaveChangesAsync();
        var repo = new KardexRepository(ctx);
        var existente = await repo.GetKardexById(1);

        var eliminado = await repo.DeleteKardex(existente!);

        eliminado.Should().BeTrue();
        (await ctx.kardex.CountAsync()).Should().Be(0);
    }
}
