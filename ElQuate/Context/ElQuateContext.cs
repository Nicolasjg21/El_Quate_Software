using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftwareSoftware.Context
{
    public class ElQuateSoftwareContext : DbContext
    {
        public ElQuateSoftwareContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Auditorias> Auditorias { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Comprobantes> Comprobantes { get; set; }
        public DbSet<Compras> Compras { get; set; }
        public DbSet<Cuentas> Cuentas { get; set; }
        public DbSet<DetalleCompras> DetalleCompras { get; set; }
        public DbSet<DetallePedidos> DetallePedidos { get; set; }
        public DbSet<Kardex> Kardex { get; set; }
        public DbSet<Mesas> Mesas { get; set; }
        public DbSet<MetodosPago> MetodosPago { get; set; }
        public DbSet<Pedidos> Pedidos { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<Proveedores> Proveedores { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<RolesPermisos> RolesPermisos { get; set; }
        public DbSet<TipoDocumento> TipoDocumento { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {
            base.OnModelCreating(modelBuilder);
            EntityConfiguration(modelBuilder);
        }

        private void EntityConfiguration(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auditorias>().ToTable("Auditorias");
            modelBuilder.Entity<Auditorias>().HasKey(u => u.idAuditoria);
            modelBuilder.Entity<Auditorias>().Property(u => u.idAuditoria).HasColumnName("idAuditoria");
            modelBuilder.Entity<Auditorias>().Property(u => u.tabla).HasColumnName("tabla");
            modelBuilder.Entity<Auditorias>().Property(u => u.accion).HasColumnName("accion");
            modelBuilder.Entity<Auditorias>().Property(u => u.fecha).HasColumnName("fecha");
            modelBuilder.Entity<Auditorias>().Property(u => u.datosAnteriores).HasColumnName("datosAnteriores");
            modelBuilder.Entity<Auditorias>().Property(u => u.datosNuevos).HasColumnName("datosNuevos");

            modelBuilder.Entity<Categorias>().ToTable("Categorias");
            modelBuilder.Entity<Categorias>().Property(u => u.idCategoria).HasColumnName("idCategoria");
            modelBuilder.Entity<Categorias>().Property(u => u.nombreCategoria).HasColumnName("nombreCategoria");

            modelBuilder.Entity<Compras>().ToTable("Compras");
            modelBuilder.Entity<Compras>().Property(u => u.idCompra).HasColumnName("idCompra");
            modelBuilder.Entity<Compras>().Property(u => u.idProveedor).HasColumnName("idProveedor");
            modelBuilder.Entity<Compras>().Property(u => u.fecha).HasColumnName("fecha");
            modelBuilder.Entity<Compras>().Property(u => u.total).HasColumnName("total");

            modelBuilder.Entity<Comprobantes>().ToTable("Comprobantes");
            modelBuilder.Entity<Comprobantes>().Property(u => u.idComprobante).HasColumnName("idComprobante");
            modelBuilder.Entity<Comprobantes>().Property(u => u.idCuenta).HasColumnName("idCuenta");
            modelBuilder.Entity<Comprobantes>().Property(u => u.fecha).HasColumnName("fecha");
            modelBuilder.Entity<Comprobantes>().Property(u => u.total).HasColumnName("total");
            modelBuilder.Entity<Comprobantes>().Property(u => u.idMetodo).HasColumnName("idMetodo");

            modelBuilder.Entity<Cuentas>().ToTable("Cuentas");
            modelBuilder.Entity<Cuentas>().Property(u => u.idCuenta).HasColumnName("idCuenta");
            modelBuilder.Entity<Cuentas>().Property(u => u.idMesa).HasColumnName("idMesa");
            modelBuilder.Entity<Cuentas>().Property(u => u.estado).HasColumnName("estado");
            modelBuilder.Entity<Cuentas>().Property(u => u.fechaApertura).HasColumnName("fechaApertura");
            modelBuilder.Entity<Cuentas>().Property(u => u.fechaCierre).HasColumnName("fechaCierre");
            modelBuilder.Entity<Cuentas>().Property(u => u.total).HasColumnName("total");

            modelBuilder.Entity<DetalleCompras>().ToTable("DetalleCompras");
            modelBuilder.Entity<DetalleCompras>().Property(u => u.idDetalleCompra).HasColumnName("idDetalleCompra");
            modelBuilder.Entity<DetalleCompras>().Property(u => u.idCompra).HasColumnName("idCompra");
            modelBuilder.Entity<DetalleCompras>().Property(u => u.idProducto).HasColumnName("idProducto");
            modelBuilder.Entity<DetalleCompras>().Property(u => u.cantidad).HasColumnName("cantidad");
            modelBuilder.Entity<DetalleCompras>().Property(u => u.precioCompra).HasColumnName("precioCompra");

            modelBuilder.Entity<DetallePedidos>().ToTable("DetallePedidos");
            modelBuilder.Entity<DetallePedidos>().Property(u => u.idDetalle).HasColumnName("idDetalle");
            modelBuilder.Entity<DetallePedidos>().Property(u => u.idPedido).HasColumnName("idPedido");
            modelBuilder.Entity<DetallePedidos>().Property(u => u.idProducto).HasColumnName("idProducto");
            modelBuilder.Entity<DetallePedidos>().Property(u => u.cantidad).HasColumnName("cantidad");
            modelBuilder.Entity<DetallePedidos>().Property(u => u.precioUnitario).HasColumnName("precioUnitario");

            modelBuilder.Entity<Kardex>().ToTable("Kardex");
            modelBuilder.Entity<Kardex>().Property(u => u.idMovimiento).HasColumnName("idMovimiento");
            modelBuilder.Entity<Kardex>().Property(u => u.idProducto).HasColumnName("idProducto");
            modelBuilder.Entity<Kardex>().Property(u => u.tipoMovimiento).HasColumnName("tipoMovimiento");
            modelBuilder.Entity<Kardex>().Property(u => u.cantidad).HasColumnName("cantidad");
            modelBuilder.Entity<Kardex>().Property(u => u.stockAnterior).HasColumnName("stockAnterior");
            modelBuilder.Entity<Kardex>().Property(u => u.stockNuevo).HasColumnName("stockNuevo");
            modelBuilder.Entity<Kardex>().Property(u => u.motivo).HasColumnName("motivo");
            modelBuilder.Entity<Kardex>().Property(u => u.fecha).HasColumnName("fecha");
            modelBuilder.Entity<Kardex>().Property(u => u.idUsuario).HasColumnName("idUsuario");

            modelBuilder.Entity<MetodosPago>().ToTable("MetodosPago");
            modelBuilder.Entity<MetodosPago>().HasKey(u => u.idMetodo);
            modelBuilder.Entity<MetodosPago>().Property(u => u.idMetodo).HasColumnName("idMetodo");
            modelBuilder.Entity<MetodosPago>().Property(u => u.nombreMetodo).HasColumnName("nombreMetodo");

            modelBuilder.Entity<Pedidos>().ToTable("Pedidos");
            modelBuilder.Entity<Pedidos>().HasKey(u => u.idPedido);
            modelBuilder.Entity<Pedidos>().Property(u => u.idPedido).HasColumnName("idPedido");
            modelBuilder.Entity<Pedidos>().Property(u => u.fecha).HasColumnName("fecha");
            modelBuilder.Entity<Pedidos>().Property(u => u.estadoPedido).HasColumnName("estadoPedido");

            modelBuilder.Entity<Permisos>().ToTable("Permisos");
            modelBuilder.Entity<Permisos>().HasKey(u => u.idPermiso);
            modelBuilder.Entity<Permisos>().Property(u => u.idPermiso).HasColumnName("idPermiso");
            modelBuilder.Entity<Permisos>().Property(u => u.nombrePermiso).HasColumnName("nombrePermiso");

            modelBuilder.Entity<Productos>().ToTable("Productos");
            modelBuilder.Entity<Productos>().HasKey(u => u.idProducto);
            modelBuilder.Entity<Productos>().Property(u => u.idProducto).HasColumnName("idProducto");
            modelBuilder.Entity<Productos>().Property(u => u.precioVenta).HasColumnName("precioVenta");
            modelBuilder.Entity<Productos>().Property(u => u.cantidadMinima).HasColumnName("cantidadMinima");
            modelBuilder.Entity<Productos>().Property(u => u.estado).HasColumnName("estado");


            modelBuilder.Entity<Proveedores>().ToTable("Proveedores");
            modelBuilder.Entity<Proveedores>().Property(u => u.idProveedor);
            modelBuilder.Entity<Proveedores>().Property(u => u.idProveedor).HasColumnName("idProvedor");
            modelBuilder.Entity<Proveedores>().Property(u => u.nombreProveedor).HasColumnName("nombreProveedor");
            modelBuilder.Entity<Proveedores>().Property(u => u.telefono).HasColumnName("telefono");
            modelBuilder.Entity<Proveedores>().Property(u => u.direccion).HasColumnName("direccion");

            modelBuilder.Entity<Roles>().ToTable("Roles");
            modelBuilder.Entity<Roles>().Property(u => u.idRol);
            modelBuilder.Entity<Roles>().Property(u => u.idRol).HasColumnName("idRol");
            modelBuilder.Entity<Roles>().Property(u => u.nombreRol).HasColumnName("nombreRol");

            modelBuilder.Entity<RolesPermisos>().ToTable("RolesPermisos");
            modelBuilder.Entity<RolesPermisos>().Property(u => u.idRol);
            modelBuilder.Entity<RolesPermisos>().Property(u => u.idPermiso).HasColumnName("");

            modelBuilder.Entity<TipoDocumento>().ToTable("TipoDocumento");
            modelBuilder.Entity<TipoDocumento>().Property(u => u.idTipoDocumento);
            modelBuilder.Entity<TipoDocumento>().Property(u => u.idTipoDocumento).HasColumnName("idTipoDocumento");
            modelBuilder.Entity<TipoDocumento>().Property(u => u.nombreTipo).HasColumnName("nombreTipo");

            modelBuilder.Entity<Usuarios>().ToTable("Usuarios");
            modelBuilder.Entity<Usuarios>().Property(u => u.idUsuario);
            modelBuilder.Entity<Usuarios>().Property(u => u.idUsuario).HasColumnName("idUsuario");
            modelBuilder.Entity<Usuarios>().Property(u => u.nombres).HasColumnName("nombres");
            modelBuilder.Entity<Usuarios>().Property(u => u.apellidos).HasColumnName("apellidos");
            modelBuilder.Entity<Usuarios>().Property(u => u.documento).HasColumnName("documento");
            modelBuilder.Entity<Usuarios>().Property(u => u.telefono).HasColumnName("telefono");
            modelBuilder.Entity<Usuarios>().Property(u => u.passwordHash).HasColumnName("passwordHash");
            modelBuilder.Entity<Usuarios>().Property(u => u.estado).HasColumnName("estado");
        }

        public async Task<bool> BoolAsync()
        {
            return await SaveChangesAsync() > 0;
        }
    }
}