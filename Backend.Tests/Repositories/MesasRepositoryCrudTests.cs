using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// MesasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class MesasRepositoryCrudTests
{
    [Fact]
    public async Task GetMesas_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.mesas.AddRange(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" }, new Mesas { idMesa = 2, numeroMesa = 3, estado = "Ocupada" });
        await ctx.SaveChangesAsync();

        var lista = await new MesasRepository(ctx).GetMesas();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMesas_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new MesasRepository(ctx).GetMesas();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMesasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.mesas.Add(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        await ctx.SaveChangesAsync();

        var encontrado = await new MesasRepository(ctx).GetMesasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.estado.Should().Be("Libre");
    }

    [Fact]
    public async Task GetMesasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new MesasRepository(ctx).GetMesasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostMesas_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new MesasRepository(ctx).PostMesas(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });

        guardado.Should().BeTrue();
        (await ctx.mesas.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutMesas_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.mesas.Add(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        await ctx.SaveChangesAsync();
        var repo = new MesasRepository(ctx);
        var existente = await repo.GetMesasById(1);
        existente!.estado = "Ocupada";

        var actualizado = await repo.PutMesas(existente);

        actualizado.Should().BeTrue();
        (await ctx.mesas.AsNoTracking().SingleAsync()).estado.Should().Be("Ocupada");
    }

    [Fact]
    public async Task PutMesas_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.mesas.Add(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        await ctx.SaveChangesAsync();
        var repo = new MesasRepository(ctx);
        var existente = await repo.GetMesasById(1);

        (await repo.PutMesas(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMesas_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.mesas.Add(new Mesas { idMesa = 1, numeroMesa = 3, estado = "Libre" });
        await ctx.SaveChangesAsync();
        var repo = new MesasRepository(ctx);
        var existente = await repo.GetMesasById(1);

        var eliminado = await repo.DeleteMesas(existente!);

        eliminado.Should().BeTrue();
        (await ctx.mesas.CountAsync()).Should().Be(0);
    }
}
