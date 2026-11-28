using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// ComprobantesRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class ComprobantesRepositoryCrudTests
{
    [Fact]
    public async Task GetComprobantes_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.comprobantes.AddRange(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m }, new Comprobantes { idComprobante = 2, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 65000m });
        await ctx.SaveChangesAsync();

        var lista = await new ComprobantesRepository(ctx).GetComprobantes();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetComprobantes_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new ComprobantesRepository(ctx).GetComprobantes();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComprobantesById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.comprobantes.Add(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m });
        await ctx.SaveChangesAsync();

        var encontrado = await new ComprobantesRepository(ctx).GetComprobantesById(1);

        encontrado.Should().NotBeNull();
        encontrado!.total.Should().Be(50000m);
    }

    [Fact]
    public async Task GetComprobantesById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new ComprobantesRepository(ctx).GetComprobantesById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostComprobantes_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new ComprobantesRepository(ctx).PostComprobantes(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m });

        guardado.Should().BeTrue();
        (await ctx.comprobantes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutComprobantes_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.comprobantes.Add(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprobantesRepository(ctx);
        var existente = await repo.GetComprobantesById(1);
        existente!.total = 65000m;

        var actualizado = await repo.PutComprobantes(existente);

        actualizado.Should().BeTrue();
        (await ctx.comprobantes.AsNoTracking().SingleAsync()).total.Should().Be(65000m);
    }

    [Fact]
    public async Task PutComprobantes_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.comprobantes.Add(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprobantesRepository(ctx);
        var existente = await repo.GetComprobantesById(1);

        (await repo.PutComprobantes(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteComprobantes_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.comprobantes.Add(new Comprobantes { idComprobante = 1, idCuenta = 1, idMetodo = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), total = 50000m });
        await ctx.SaveChangesAsync();
        var repo = new ComprobantesRepository(ctx);
        var existente = await repo.GetComprobantesById(1);

        var eliminado = await repo.DeleteComprobantes(existente!);

        eliminado.Should().BeTrue();
        (await ctx.comprobantes.CountAsync()).Should().Be(0);
    }
}
