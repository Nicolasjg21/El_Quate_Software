using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>CB-16 · Repositorio real con una base EF Core en memoria (fake).</summary>
public class ProveedoresRepositoryTests
{
    private static ElQuateDePattyContext CrearContexto() =>
        new(new DbContextOptionsBuilder<ElQuateDePattyContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())   // base nueva y aislada por prueba
            .Options);

    [Fact]
    public async Task GetProveedoresById_Existe_LoEncuentra()
    {
        // Arrange
        using var ctx = CrearContexto();
        ctx.proveedores.Add(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria", telefono = "300", direccion = "Cra 1" });
        await ctx.SaveChangesAsync();
        var repo = new ProveedoresRepository(ctx);

        // Act
        var proveedor = await repo.GetProveedoresById(1);

        // Assert
        proveedor.Should().NotBeNull();
        proveedor!.nombreProveedor.Should().Be("Bavaria");
    }

    [Fact]
    public async Task GetProveedoresById_NoExiste_RetornaNull()
    {
        using var ctx = CrearContexto();
        var repo = new ProveedoresRepository(ctx);

        var proveedor = await repo.GetProveedoresById(99);

        proveedor.Should().BeNull();
    }

    [Fact]
    public async Task PostProveedores_Valido_LoGuarda()
    {
        using var ctx = CrearContexto();
        var repo = new ProveedoresRepository(ctx);

        var guardado = await repo.PostProveedores(new Proveedores { nombreProveedor = "Postobón", telefono = "301", direccion = "Cl 2" });

        guardado.Should().BeTrue();
        ctx.proveedores.Should().ContainSingle(p => p.nombreProveedor == "Postobón");
    }
}
