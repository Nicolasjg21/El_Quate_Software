using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// ProveedoresRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class ProveedoresRepositoryCrudTests
{
    [Fact]
    public async Task GetProveedores_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.proveedores.AddRange(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" }, new Proveedores { idProveedor = 2, telefono = "300", direccion = "Cra 1", nombreProveedor = "Postobón" });
        await ctx.SaveChangesAsync();

        var lista = await new ProveedoresRepository(ctx).GetProveedores();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProveedores_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new ProveedoresRepository(ctx).GetProveedores();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProveedoresById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.proveedores.Add(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" });
        await ctx.SaveChangesAsync();

        var encontrado = await new ProveedoresRepository(ctx).GetProveedoresById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreProveedor.Should().Be("Bavaria");
    }

    [Fact]
    public async Task GetProveedoresById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new ProveedoresRepository(ctx).GetProveedoresById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostProveedores_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new ProveedoresRepository(ctx).PostProveedores(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" });

        guardado.Should().BeTrue();
        (await ctx.proveedores.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutProveedores_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.proveedores.Add(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" });
        await ctx.SaveChangesAsync();
        var repo = new ProveedoresRepository(ctx);
        var existente = await repo.GetProveedoresById(1);
        existente!.nombreProveedor = "Postobón";

        var actualizado = await repo.PutProveedores(existente);

        actualizado.Should().BeTrue();
        (await ctx.proveedores.AsNoTracking().SingleAsync()).nombreProveedor.Should().Be("Postobón");
    }

    [Fact]
    public async Task PutProveedores_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.proveedores.Add(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" });
        await ctx.SaveChangesAsync();
        var repo = new ProveedoresRepository(ctx);
        var existente = await repo.GetProveedoresById(1);

        (await repo.PutProveedores(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProveedores_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.proveedores.Add(new Proveedores { idProveedor = 1, telefono = "300", direccion = "Cra 1", nombreProveedor = "Bavaria" });
        await ctx.SaveChangesAsync();
        var repo = new ProveedoresRepository(ctx);
        var existente = await repo.GetProveedoresById(1);

        var eliminado = await repo.DeleteProveedores(existente!);

        eliminado.Should().BeTrue();
        (await ctx.proveedores.CountAsync()).Should().Be(0);
    }
}
