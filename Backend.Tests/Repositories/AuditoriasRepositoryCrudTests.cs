using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// AuditoriasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class AuditoriasRepositoryCrudTests
{
    [Fact]
    public async Task GetAuditorias_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.auditorias.AddRange(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" }, new Auditorias { idAuditoria = 2, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "UPDATE" });
        await ctx.SaveChangesAsync();

        var lista = await new AuditoriasRepository(ctx).GetAuditorias();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAuditorias_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new AuditoriasRepository(ctx).GetAuditorias();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditoriasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.auditorias.Add(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" });
        await ctx.SaveChangesAsync();

        var encontrado = await new AuditoriasRepository(ctx).GetAuditoriasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.accion.Should().Be("INSERT");
    }

    [Fact]
    public async Task GetAuditoriasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new AuditoriasRepository(ctx).GetAuditoriasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostAuditorias_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new AuditoriasRepository(ctx).PostAuditorias(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" });

        guardado.Should().BeTrue();
        (await ctx.auditorias.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutAuditorias_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.auditorias.Add(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" });
        await ctx.SaveChangesAsync();
        var repo = new AuditoriasRepository(ctx);
        var existente = await repo.GetAuditoriasById(1);
        existente!.accion = "UPDATE";

        var actualizado = await repo.PutAuditorias(existente);

        actualizado.Should().BeTrue();
        (await ctx.auditorias.AsNoTracking().SingleAsync()).accion.Should().Be("UPDATE");
    }

    [Fact]
    public async Task PutAuditorias_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.auditorias.Add(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" });
        await ctx.SaveChangesAsync();
        var repo = new AuditoriasRepository(ctx);
        var existente = await repo.GetAuditoriasById(1);

        (await repo.PutAuditorias(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAuditorias_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.auditorias.Add(new Auditorias { idAuditoria = 1, tabla = "Productos", idUsuario = 1, fecha = new DateTime(2026, 11, 5, 20, 0, 0), accion = "INSERT" });
        await ctx.SaveChangesAsync();
        var repo = new AuditoriasRepository(ctx);
        var existente = await repo.GetAuditoriasById(1);

        var eliminado = await repo.DeleteAuditorias(existente!);

        eliminado.Should().BeTrue();
        (await ctx.auditorias.CountAsync()).Should().Be(0);
    }
}
