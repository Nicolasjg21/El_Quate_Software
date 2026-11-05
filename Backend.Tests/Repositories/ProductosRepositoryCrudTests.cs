using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>
/// ProductosRepository contra una base EF Core en memoria (fake), una base nueva por prueba.
/// Verifica que cada operación realmente lee, guarda, actualiza y borra en la base.
/// </summary>
public class ProductosRepositoryCrudTests
{
    [Fact]
    public async Task GetProductos_ConRegistros_LosDevuelveTodos()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.productos.AddRange(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" }, new Productos { idProducto = 2, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });
        await ctx.SaveChangesAsync();

        var lista = await new ProductosRepository(ctx).GetProductos();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProductos_SinRegistros_DevuelveListaVacia()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var lista = await new ProductosRepository(ctx).GetProductos();

        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductosById_Existe_LoEncuentra()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.productos.Add(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });
        await ctx.SaveChangesAsync();

        var encontrado = await new ProductosRepository(ctx).GetProductosById(1);

        encontrado.Should().NotBeNull();
        encontrado!.nombreProducto.Should().Be("Cerveza Águila");
    }

    [Fact]
    public async Task GetProductosById_NoExiste_DevuelveNull()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await new ProductosRepository(ctx).GetProductosById(99)).Should().BeNull();
    }

    [Fact]
    public async Task PostProductos_Valido_LoGuardaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();

        var guardado = await new ProductosRepository(ctx).PostProductos(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        guardado.Should().BeTrue();
        (await ctx.productos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PutProductos_CambioDeDatos_QuedaGuardado()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.productos.Add(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });
        await ctx.SaveChangesAsync();
        var repo = new ProductosRepository(ctx);
        var existente = await repo.GetProductosById(1);
        existente!.nombreProducto = "Cerveza Poker";

        var actualizado = await repo.PutProductos(existente);

        actualizado.Should().BeTrue();
        (await ctx.productos.AsNoTracking().SingleAsync()).nombreProducto.Should().Be("Cerveza Poker");
    }

    [Fact]
    public async Task PutProductos_SinCambios_IgualDevuelveTrue()
    {
        // Hallazgo de caja blanca: el método ignora el resultado de SaveChanges y siempre devuelve true,
        // por eso la rama "No fue posible actualizar" del controlador nunca se ejecuta en la práctica.
        using var ctx = BaseDatosPrueba.Crear();
        ctx.productos.Add(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });
        await ctx.SaveChangesAsync();
        var repo = new ProductosRepository(ctx);
        var existente = await repo.GetProductosById(1);

        (await repo.PutProductos(existente!)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductos_Existe_LoEliminaYDevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.productos.Add(new Productos { idProducto = 1, precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });
        await ctx.SaveChangesAsync();
        var repo = new ProductosRepository(ctx);
        var existente = await repo.GetProductosById(1);

        var eliminado = await repo.DeleteProductos(existente!);

        eliminado.Should().BeTrue();
        (await ctx.productos.CountAsync()).Should().Be(0);
    }
}
