using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Backend.Tests.Context;

/// <summary>
/// ElQuateDePattyContext y los modelos: la configuración de EF Core (Fluent API + DataAnnotations)
/// que define tablas, llaves, relaciones, precisión de dinero y longitudes. Los modelos no tienen
/// lógica propia: lo que se prueba de ellos es cómo quedan mapeados a la base de datos.
/// </summary>
public class ContextoYModelosTests
{
    private static IModel Modelo()
    {
        using var ctx = BaseDatosPrueba.Crear();
        return ctx.Model;
    }

    public static TheoryData<Type, string, string[]> Tablas => new()
    {
        { typeof(Auditorias), "Auditorias", new[] { "idAuditoria" } },
        { typeof(Categorias), "Categorias", new[] { "idCategoria" } },
        { typeof(Comprobantes), "Comprobantes", new[] { "idComprobante" } },
        { typeof(Compras), "Compras", new[] { "idCompra" } },
        { typeof(Cuentas), "Cuentas", new[] { "idCuenta" } },
        { typeof(DetalleCompras), "DetalleCompras", new[] { "idDetalleCompra" } },
        { typeof(DetallePedidos), "DetallePedidos", new[] { "idDetalle" } },
        { typeof(Kardex), "Kardex", new[] { "idMovimiento" } },
        { typeof(Mesas), "Mesas", new[] { "idMesa" } },
        { typeof(MetodosPago), "MetodosPago", new[] { "idMetodo" } },
        { typeof(Pedidos), "Pedidos", new[] { "idPedido" } },
        { typeof(Permisos), "Permisos", new[] { "idPermiso" } },
        { typeof(Productos), "Productos", new[] { "idProducto" } },
        { typeof(Proveedores), "Proveedores", new[] { "idProveedor" } },
        { typeof(Roles), "Roles", new[] { "idRol" } },
        { typeof(RolesPermisos), "RolesPermisos", new[] { "idRol", "idPermiso" } },
        { typeof(TipoDocumento), "TipoDocumento", new[] { "idTipoDocumento" } },
        { typeof(Usuarios), "Usuarios", new[] { "idUsuario" } },
    };

    [Fact]
    public void Contexto_Tiene18Entidades()
    {
        Modelo().GetEntityTypes().Should().HaveCount(18);
    }

    [Theory]
    [MemberData(nameof(Tablas))]
    public void Entidad_SeMapeaASuTablaConSuLlavePrimaria(Type tipo, string tabla, string[] llave)
    {
        var entidad = Modelo().FindEntityType(tipo)!;

        entidad.GetTableName().Should().Be(tabla);
        entidad.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Equal(llave);
    }

    [Fact]
    public void Relaciones_NingunaBorraEnCascada()
    {
        // Protege el historial: no se puede borrar un usuario, producto o cuenta y perder sus ventas en cadena.
        var relaciones = Modelo().GetEntityTypes().SelectMany(e => e.GetForeignKeys()).ToList();

        relaciones.Should().NotBeEmpty();
        relaciones.Should().AllSatisfy(fk => fk.DeleteBehavior.Should().NotBe(DeleteBehavior.Cascade,
            $"{fk.DeclaringEntityType.ClrType.Name} → {fk.PrincipalEntityType.ClrType.Name} no debe borrar en cascada"));
    }

    [Theory]
    [InlineData(typeof(Compras), "total")]
    [InlineData(typeof(Cuentas), "total")]
    [InlineData(typeof(Comprobantes), "total")]
    [InlineData(typeof(DetalleCompras), "precioCompra")]
    [InlineData(typeof(DetallePedidos), "precioUnitario")]
    [InlineData(typeof(Productos), "precioVenta")]
    public void Dinero_SeGuardaConPrecision10Y2(Type tipo, string propiedad)
    {
        var p = Modelo().FindEntityType(tipo)!.FindProperty(propiedad)!;

        p.GetPrecision().Should().Be(10);
        p.GetScale().Should().Be(2);
    }

    [Theory]
    [InlineData(typeof(Auditorias), "tabla", 50)]
    [InlineData(typeof(Auditorias), "accion", 50)]
    [InlineData(typeof(Categorias), "nombreCategoria", 100)]
    [InlineData(typeof(Cuentas), "estado", 20)]
    [InlineData(typeof(Kardex), "tipoMovimiento", 10)]
    [InlineData(typeof(Mesas), "estado", 20)]
    [InlineData(typeof(MetodosPago), "nombreMetodo", 50)]
    [InlineData(typeof(Pedidos), "estadoPedido", 20)]
    [InlineData(typeof(Permisos), "nombrePermiso", 100)]
    [InlineData(typeof(Proveedores), "telefono", 20)]
    [InlineData(typeof(Proveedores), "direccion", 150)]
    [InlineData(typeof(Roles), "nombreRol", 50)]
    public void Texto_TieneLaLongitudMaximaDelModelo(Type tipo, string propiedad, int maximo)
    {
        Modelo().FindEntityType(tipo)!.FindProperty(propiedad)!.GetMaxLength().Should().Be(maximo);
    }

    [Theory]
    [InlineData(typeof(Pedidos), typeof(Cuentas))]
    [InlineData(typeof(Pedidos), typeof(Usuarios))]
    [InlineData(typeof(DetallePedidos), typeof(Pedidos))]
    [InlineData(typeof(DetallePedidos), typeof(Productos))]
    [InlineData(typeof(Cuentas), typeof(Mesas))]
    [InlineData(typeof(Comprobantes), typeof(Cuentas))]
    [InlineData(typeof(Comprobantes), typeof(MetodosPago))]
    [InlineData(typeof(Compras), typeof(Proveedores))]
    [InlineData(typeof(DetalleCompras), typeof(Compras))]
    [InlineData(typeof(DetalleCompras), typeof(Productos))]
    [InlineData(typeof(Productos), typeof(Categorias))]
    [InlineData(typeof(Kardex), typeof(Productos))]
    [InlineData(typeof(Kardex), typeof(Usuarios))]
    [InlineData(typeof(Auditorias), typeof(Usuarios))]
    [InlineData(typeof(Usuarios), typeof(Roles))]
    [InlineData(typeof(Usuarios), typeof(TipoDocumento))]
    [InlineData(typeof(RolesPermisos), typeof(Roles))]
    [InlineData(typeof(RolesPermisos), typeof(Permisos))]
    public void Relacion_ExisteLaLlaveForanea(Type dependiente, Type principal)
    {
        Modelo().FindEntityType(dependiente)!.GetForeignKeys()
            .Should().Contain(fk => fk.PrincipalEntityType.ClrType == principal);
    }

    [Fact]
    public async Task BoolAsync_ConCambios_DevuelveTrue()
    {
        using var ctx = BaseDatosPrueba.Crear();
        ctx.categorias.Add(new Categorias { nombreCategoria = "Cervezas" });

        (await ctx.BoolAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task BoolAsync_SinCambios_DevuelveFalse()
    {
        using var ctx = BaseDatosPrueba.Crear();

        (await ctx.BoolAsync()).Should().BeFalse();
    }
}
