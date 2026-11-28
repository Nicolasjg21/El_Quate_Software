using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// TipoDocumentoRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class TipoDocumentoRepositoryCrudTests
{
    [Fact]
    public async Task GetTipoDocumento_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.tipoDocumento.AddRange(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" }, new TipoDocumento { idTipoDocumento = 2, nombreTipo = "Pasaporte" });
        await ctx.SaveChangesAsync();

        var lista = await new TipoDocumentoRepository(ctx).GetTipoDocumento();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTipoDocumento_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new TipoDocumentoRepository(ctx).GetTipoDocumento();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTipoDocumentoById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.tipoDocumento.Add(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });
        await ctx.SaveChangesAsync();

        var encontrado = await new TipoDocumentoRepository(ctx).GetTipoDocumentoById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreTipo.Should().Be("Cédula");
    }

    [Fact]
    public async Task GetTipoDocumentoById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new TipoDocumentoRepository(ctx).GetTipoDocumentoById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostTipoDocumento_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new TipoDocumentoRepository(ctx).PostTipoDocumento(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });

        guardado.Should().BeTrue();
        (await ctx.tipoDocumento.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutTipoDocumento_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.tipoDocumento.Add(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });
        await ctx.SaveChangesAsync();
        var repo = new TipoDocumentoRepository(ctx);
        var existente = await repo.GetTipoDocumentoById(1);
        existente!.nombreTipo = "Pasaporte";

        var actualizado = await repo.PutTipoDocumento(existente);

        actualizado.Should().BeTrue();
        (await ctx.tipoDocumento.AsNoTracking().SingleAsync()).nombreTipo.Should().Be("Pasaporte");
    }

    [Fact]
    public async Task PutTipoDocumento_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.tipoDocumento.Add(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });
        await ctx.SaveChangesAsync();
        var repo = new TipoDocumentoRepository(ctx);
        var existente = await repo.GetTipoDocumentoById(1);

        (await repo.PutTipoDocumento(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTipoDocumento_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.tipoDocumento.Add(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });
        await ctx.SaveChangesAsync();
        var repo = new TipoDocumentoRepository(ctx);
        var existente = await repo.GetTipoDocumentoById(1);

        var eliminado = await repo.DeleteTipoDocumento(existente!);

        eliminado.Should().BeTrue();
        (await ctx.tipoDocumento.CountAsync()).Should().Be(0);
    }
}
