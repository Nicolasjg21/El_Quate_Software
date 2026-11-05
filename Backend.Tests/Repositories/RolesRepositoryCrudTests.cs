using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// RolesRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class RolesRepositoryCrudTests
{
    [Fact]
    public async Task GetRoles_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.roles.AddRange(new Roles { idRol = 1, nombreRol = "Mesero" }, new Roles { idRol = 2, nombreRol = "Cajero" });
        await ctx.SaveChangesAsync();

        var lista = await new RolesRepository(ctx).GetRoles();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRoles_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new RolesRepository(ctx).GetRoles();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.roles.Add(new Roles { idRol = 1, nombreRol = "Mesero" });
        await ctx.SaveChangesAsync();

        var encontrado = await new RolesRepository(ctx).GetRolesById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreRol.Should().Be("Mesero");
    }

    [Fact]
    public async Task GetRolesById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new RolesRepository(ctx).GetRolesById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostRoles_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new RolesRepository(ctx).PostRoles(new Roles { idRol = 1, nombreRol = "Mesero" });

        guardado.Should().BeTrue();
        (await ctx.roles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutRoles_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.roles.Add(new Roles { idRol = 1, nombreRol = "Mesero" });
        await ctx.SaveChangesAsync();
        var repo = new RolesRepository(ctx);
        var existente = await repo.GetRolesById(1);
        existente!.nombreRol = "Cajero";

        var actualizado = await repo.PutRoles(existente);

        actualizado.Should().BeTrue();
        (await ctx.roles.AsNoTracking().SingleAsync()).nombreRol.Should().Be("Cajero");
    }

    [Fact]
    public async Task PutRoles_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.roles.Add(new Roles { idRol = 1, nombreRol = "Mesero" });
        await ctx.SaveChangesAsync();
        var repo = new RolesRepository(ctx);
        var existente = await repo.GetRolesById(1);

        (await repo.PutRoles(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRoles_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.roles.Add(new Roles { idRol = 1, nombreRol = "Mesero" });
        await ctx.SaveChangesAsync();
        var repo = new RolesRepository(ctx);
        var existente = await repo.GetRolesById(1);

        var eliminado = await repo.DeleteRoles(existente!);

        eliminado.Should().BeTrue();
        (await ctx.roles.CountAsync()).Should().Be(0);
    }
}
