using ElQuateDePatty.Context;
using ElQuateDePatty.Models;

namespace Backend.Tests.Ayudas;

/// <summary>
/// Datos de prueba coherentes de un bar, para verificar filtros y cálculos con resultados conocidos.
///
/// Usuarios: 1 Admin (activo), 2 Mesero (activo), 3 Inactivo.
/// Mesas: 1 (Libre), 2 (Ocupada), 3 (Ocupada).
/// Cuentas: 1 = mesa 1, Abierta, $0 · 2 = mesa 3, Cerrada, $30.000 · 3 = mesa 1, Cerrada, $90.000.
/// Comprobantes: cuenta 2 → $30.000 en Efectivo (3 nov) · cuenta 3 → $90.000 por Nequi (5 nov).
/// Pedidos: 1 = cuenta 1, mesero, 5 nov 21:00 · 2 = cuenta 2, mesero, 3 nov 19:30 · 3 = cuenta 3, admin, 5 nov 15:00.
/// Productos: 1 Cerveza Águila ($5.000, mín 10) · 2 Aguardiente ($60.000, mín 2) · 3 Cerveza Club ($6.000, mín 5).
/// Vendidos: pedido 1 → 2 Águila · pedido 2 → 6 Águila · pedido 3 → 1 Aguardiente + 5 Club.
/// Compras: 1 = Bavaria, 1 nov: 24 Águila a $3.000 + 12 Club a $4.000 (36 artículos, $120.000)
///          2 = Dislicores, 4 nov: 5 Aguardiente a $40.000 (5 artículos, $200.000).
/// Stock (último kardex): Águila 16 · Aguardiente 2 (= mínimo, stock bajo) · Club sin movimientos (0).
/// </summary>
public static class EscenarioBar
{
    public static readonly DateTime Dia3 = new(2026, 11, 3);
    public static readonly DateTime Dia5 = new(2026, 11, 5);

    public static ElQuateDePattyContext Crear()
    {
        var ctx = BaseDatosPrueba.Crear();

        ctx.tipoDocumento.Add(new TipoDocumento { idTipoDocumento = 1, nombreTipo = "Cédula" });
        ctx.roles.AddRange(new Roles { idRol = 1, nombreRol = "Administrador" }, new Roles { idRol = 2, nombreRol = "Mesero" });
        ctx.permisos.AddRange(new Permisos { idPermiso = 1, nombrePermiso = "ventas.cobrar" },
                              new Permisos { idPermiso = 2, nombrePermiso = "usuarios.gestionar" });
        ctx.rolesPermisos.AddRange(new RolesPermisos { idRol = 1, idPermiso = 1 },
                                   new RolesPermisos { idRol = 1, idPermiso = 2 },
                                   new RolesPermisos { idRol = 2, idPermiso = 1 });

        ctx.usuarios.AddRange(
            Usuario(1, "Ana", "admin@elcuate.com", 1, true),
            Usuario(2, "Luis", "mesero@elcuate.com", 2, true),
            Usuario(3, "Pedro", "inactivo@elcuate.com", 2, false));

        ctx.mesas.AddRange(new Mesas { idMesa = 1, numeroMesa = 1, estado = "Libre" },
                           new Mesas { idMesa = 2, numeroMesa = 2, estado = "Ocupada" },
                           new Mesas { idMesa = 3, numeroMesa = 3, estado = "Ocupada" });

        ctx.cuentas.AddRange(
            new Cuentas { idCuenta = 1, idMesa = 1, estado = "Abierta", fechaApertura = Dia5.AddHours(20), total = 0m },
            new Cuentas { idCuenta = 2, idMesa = 3, estado = "Cerrada", fechaApertura = Dia3.AddHours(19), fechaCierre = Dia3.AddHours(22), total = 30000m },
            new Cuentas { idCuenta = 3, idMesa = 1, estado = "Cerrada", fechaApertura = Dia5.AddHours(14), fechaCierre = Dia5.AddHours(16), total = 90000m });

        ctx.metodosPago.AddRange(new MetodosPago { idMetodo = 1, nombreMetodo = "Efectivo" },
                                 new MetodosPago { idMetodo = 2, nombreMetodo = "Nequi" });
        ctx.comprobantes.AddRange(
            new Comprobantes { idComprobante = 1, idCuenta = 2, idMetodo = 1, fecha = Dia3.AddHours(22), total = 30000m },
            new Comprobantes { idComprobante = 2, idCuenta = 3, idMetodo = 2, fecha = Dia5.AddHours(16), total = 90000m });

        ctx.pedidos.AddRange(
            new Pedidos { idPedido = 1, idCuenta = 1, idUsuario = 2, fecha = Dia5.AddHours(21), estadoPedido = "Pendiente" },
            new Pedidos { idPedido = 2, idCuenta = 2, idUsuario = 2, fecha = Dia3.AddHours(19).AddMinutes(30), estadoPedido = "Entregado" },
            new Pedidos { idPedido = 3, idCuenta = 3, idUsuario = 1, fecha = Dia5.AddHours(15), estadoPedido = "Entregado" });

        ctx.categorias.AddRange(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" },
                                new Categorias { idCategoria = 2, nombreCategoria = "Licores" });
        ctx.productos.AddRange(
            new Productos { idProducto = 1, nombreProducto = "Cerveza Águila", precioVenta = 5000m, cantidadMinima = 10, estado = true, idCategoria = 1 },
            new Productos { idProducto = 2, nombreProducto = "Aguardiente", precioVenta = 60000m, cantidadMinima = 2, estado = true, idCategoria = 2 },
            new Productos { idProducto = 3, nombreProducto = "Cerveza Club", precioVenta = 6000m, cantidadMinima = 5, estado = true, idCategoria = 1 });

        ctx.detallePedidos.AddRange(
            new DetallePedidos { idDetalle = 1, idPedido = 1, idProducto = 1, cantidad = 2, precioUnitario = 5000m },
            new DetallePedidos { idDetalle = 2, idPedido = 2, idProducto = 1, cantidad = 6, precioUnitario = 5000m },
            new DetallePedidos { idDetalle = 3, idPedido = 3, idProducto = 2, cantidad = 1, precioUnitario = 60000m },
            new DetallePedidos { idDetalle = 4, idPedido = 3, idProducto = 3, cantidad = 5, precioUnitario = 6000m });

        ctx.proveedores.AddRange(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1 # 2-3" },
                                 new Proveedores { idProveedor = 2, nombreProveedor = "Dislicores", telefono = "3109876543", direccion = "Calle 80 # 10-20" });
        ctx.compras.AddRange(new Compras { idCompra = 1, idProveedor = 1, fecha = new DateTime(2026, 11, 1), total = 120000m },
                             new Compras { idCompra = 2, idProveedor = 2, fecha = new DateTime(2026, 11, 4), total = 200000m });
        ctx.detalleCompras.AddRange(
            new DetalleCompras { idDetalleCompra = 1, idCompra = 1, idProducto = 1, cantidad = 24, precioCompra = 3000m },
            new DetalleCompras { idDetalleCompra = 2, idCompra = 1, idProducto = 3, cantidad = 12, precioCompra = 4000m },
            new DetalleCompras { idDetalleCompra = 3, idCompra = 2, idProducto = 2, cantidad = 5, precioCompra = 40000m });

        ctx.kardex.AddRange(
            Mov(1, 1, "ENTRADA", 24, 0, 24, new DateTime(2026, 11, 1)),
            Mov(2, 1, "SALIDA", 8, 24, 16, Dia5),
            Mov(3, 2, "ENTRADA", 5, 0, 5, new DateTime(2026, 11, 4)),
            Mov(4, 2, "SALIDA", 3, 5, 2, Dia5));

        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return ctx;
    }

    private static Usuarios Usuario(int id, string nombre, string email, int idRol, bool activo) => new()
    {
        idUsuario = id, nombres = nombre, apellidos = "Prueba", documento = $"10{id}", idTipoDocumento = 1,
        telefono = "300", passwordHash = $"HASH-{id}", estado = activo, idRol = idRol, email = email
    };

    private static Kardex Mov(int id, int idProducto, string tipo, int cantidad, int antes, int despues, DateTime fecha) => new()
    {
        idMovimiento = id, idProducto = idProducto, tipoMovimiento = tipo, cantidad = cantidad,
        stockAnterior = antes, stockNuevo = despues, fecha = fecha, idUsuario = 1, motivo = "Prueba"
    };
}
