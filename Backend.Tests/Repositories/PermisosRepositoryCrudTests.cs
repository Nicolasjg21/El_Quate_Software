using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// PermisosRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class PermisosRepositoryCrudTests
{
    [Fact]
    public async Task GetPermisos_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.permisos.AddRange(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" }, new Permisos { idPermiso = 2, nombrePermiso = "ventas.anular" });
        await ctx.SaveChangesAsync();

        var lista = await new PermisosRepository(ctx).GetPermisos();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPermisos_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new PermisosRepository(ctx).GetPermisos();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermisosById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.permisos.Add(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        await ctx.SaveChangesAsync();

        var encontrado = await new PermisosRepository(ctx).GetPermisosById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombrePermiso.Should().Be("ventas.cobrar");
    }

    [Fact]
    public async Task GetPermisosById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new PermisosRepository(ctx).GetPermisosById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostPermisos_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new PermisosRepository(ctx).PostPermisos(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });

        guardado.Should().BeTrue();
        (await ctx.permisos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutPermisos_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.permisos.Add(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        await ctx.SaveChangesAsync();
        var repo = new PermisosRepository(ctx);
        var existente = await repo.GetPermisosById(1);
        existente!.nombrePermiso = "ventas.anular";

        var actualizado = await repo.PutPermisos(existente);

        actualizado.Should().BeTrue();
        (await ctx.permisos.AsNoTracking().SingleAsync()).nombrePermiso.Should().Be("ventas.anular");
    }

    [Fact]
    public async Task PutPermisos_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.permisos.Add(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        await ctx.SaveChangesAsync();
        var repo = new PermisosRepository(ctx);
        var existente = await repo.GetPermisosById(1);

        (await repo.PutPermisos(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeletePermisos_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.permisos.Add(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" });
        await ctx.SaveChangesAsync();
        var repo = new PermisosRepository(ctx);
        var existente = await repo.GetPermisosById(1);

        var eliminado = await repo.DeletePermisos(existente!);

        eliminado.Should().BeTrue();
        (await ctx.permisos.CountAsync()).Should().Be(0);
    }
}
