using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// CategoriasRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class CategoriasRepositoryCrudTests
{
    [Fact]
    public async Task GetCategorias_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.AddRange(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" }, new Categorias { idCategoria = 2, nombreCategoria = "Licores" });
        await ctx.SaveChangesAsync();

        var lista = await new CategoriasRepository(ctx).GetCategorias();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategorias_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new CategoriasRepository(ctx).GetCategorias();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoriasById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.Add(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        await ctx.SaveChangesAsync();

        var encontrado = await new CategoriasRepository(ctx).GetCategoriasById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreCategoria.Should().Be("Cervezas");
    }

    [Fact]
    public async Task GetCategoriasById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new CategoriasRepository(ctx).GetCategoriasById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostCategorias_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new CategoriasRepository(ctx).PostCategorias(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });

        guardado.Should().BeTrue();
        (await ctx.categorias.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutCategorias_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.Add(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        await ctx.SaveChangesAsync();
        var repo = new CategoriasRepository(ctx);
        var existente = await repo.GetCategoriasById(1);
        existente!.nombreCategoria = "Licores";

        var actualizado = await repo.PutCategorias(existente);

        actualizado.Should().BeTrue();
        (await ctx.categorias.AsNoTracking().SingleAsync()).nombreCategoria.Should().Be("Licores");
    }

    [Fact]
    public async Task PutCategorias_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.Add(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        await ctx.SaveChangesAsync();
        var repo = new CategoriasRepository(ctx);
        var existente = await repo.GetCategoriasById(1);

        (await repo.PutCategorias(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategorias_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.Add(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        await ctx.SaveChangesAsync();
        var repo = new CategoriasRepository(ctx);
        var existente = await repo.GetCategoriasById(1);

        var eliminado = await repo.DeleteCategorias(existente!);

        eliminado.Should().BeTrue();
        (await ctx.categorias.CountAsync()).Should().Be(0);
    }
}
