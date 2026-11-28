using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// CuentasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class CuentasRepositoryCrudTests
{
    [Fact]
    public async Task GetCuentas_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.cuentas.AddRange(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" }, new Cuentas { idCuenta = 2, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Cerrada" });
        await ctx.SaveChangesAsync();

        var lista = await new CuentasRepository(ctx).GetCuentas();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCuentas_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new CuentasRepository(ctx).GetCuentas();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCuentasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" });
        await ctx.SaveChangesAsync();

        var encontrado = await new CuentasRepository(ctx).GetCuentasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.estado.Should().Be("Abierta");
    }

    [Fact]
    public async Task GetCuentasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new CuentasRepository(ctx).GetCuentasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostCuentas_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new CuentasRepository(ctx).PostCuentas(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" });

        guardado.Should().BeTrue();
        (await ctx.cuentas.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutCuentas_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" });
        await ctx.SaveChangesAsync();
        var repo = new CuentasRepository(ctx);
        var existente = await repo.GetCuentasById(1);
        existente!.estado = "Cerrada";

        var actualizado = await repo.PutCuentas(existente);

        actualizado.Should().BeTrue();
        (await ctx.cuentas.AsNoTracking().SingleAsync()).estado.Should().Be("Cerrada");
    }

    [Fact]
    public async Task PutCuentas_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" });
        await ctx.SaveChangesAsync();
        var repo = new CuentasRepository(ctx);
        var existente = await repo.GetCuentasById(1);

        (await repo.PutCuentas(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCuentas_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.cuentas.Add(new Cuentas { idCuenta = 1, idMesa = 1, fechaApertura = new DateTime(2026, 11, 5, 20, 0, 0), total = 0m, estado = "Abierta" });
        await ctx.SaveChangesAsync();
        var repo = new CuentasRepository(ctx);
        var existente = await repo.GetCuentasById(1);

        var eliminado = await repo.DeleteCuentas(existente!);

        eliminado.Should().BeTrue();
        (await ctx.cuentas.CountAsync()).Should().Be(0);
    }
}
