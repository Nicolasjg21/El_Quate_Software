using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Context
{
    public class ElQuateDePattyContext : DbContext
    {
        public ElQuateDePattyContext(DbContextOptions options) : base(options)
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
            // =========================================================
            // Auditorias
            // =========================================================

            modelBuilder.Entity<Auditorias>(entity =>
            {
                entity.ToTable("Auditorias");

                entity.HasKey(x => x.idAuditoria);

                entity.Property(x => x.idAuditoria)
                    .HasColumnName("idAuditoria");

                entity.Property(x => x.tabla)
                    .HasColumnName("tabla");

                entity.Property(x => x.accion)
                    .HasColumnName("accion");

                entity.Property(x => x.idUsuario)
                    .HasColumnName("idUsuario");

                entity.Property(x => x.fecha)
                    .HasColumnName("fecha");

                entity.Property(x => x.datosAnteriores)
                    .HasColumnName("datosAnteriores");

                entity.Property(x => x.datosNuevos)
                    .HasColumnName("datosNuevos");

                entity.HasOne(x => x.usuario)
                    .WithMany(x => x.auditorias)
                    .HasForeignKey(x => x.idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Categorias
            // =========================================================

            modelBuilder.Entity<Categorias>(entity =>
            {
                entity.ToTable("Categorias");

                entity.HasKey(x => x.idCategoria);

                entity.Property(x => x.idCategoria)
                    .HasColumnName("idCategoria");

                entity.Property(x => x.nombreCategoria)
                    .HasColumnName("nombreCategoria");
            });


            // =========================================================
            // Proveedores
            // =========================================================

            modelBuilder.Entity<Proveedores>(entity =>
            {
                entity.ToTable("Proveedores");

                entity.HasKey(x => x.idProveedor);

                entity.Property(x => x.idProveedor)
                    .HasColumnName("idProveedor");

                entity.Property(x => x.nombreProveedor)
                    .HasColumnName("nombreProveedor");

                entity.Property(x => x.telefono)
                    .HasColumnName("telefono");

                entity.Property(x => x.direccion)
                    .HasColumnName("direccion");
            });


            // =========================================================
            // Compras
            // =========================================================

            modelBuilder.Entity<Compras>(entity =>
            {
                entity.ToTable("Compras");

                entity.HasKey(x => x.idCompra);

                entity.Property(x => x.idCompra)
                    .HasColumnName("idCompra");

                entity.Property(x => x.idProveedor)
                    .HasColumnName("idProveedor");

                entity.Property(x => x.fecha)
                    .HasColumnName("fecha");

                entity.Property(x => x.total)
                    .HasColumnName("total")
                    .HasPrecision(10, 2);

                entity.HasOne(x => x.proveedor)
                    .WithMany(x => x.compras)
                    .HasForeignKey(x => x.idProveedor)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Mesas
            // =========================================================

            modelBuilder.Entity<Mesas>(entity =>
            {
                entity.ToTable("Mesas");

                entity.HasKey(x => x.idMesa);

                entity.Property(x => x.idMesa)
                    .HasColumnName("idMesa");

                entity.Property(x => x.numeroMesa)
                    .HasColumnName("numeroMesa");

                entity.Property(x => x.estado)
                    .HasColumnName("estado");
            });


            // =========================================================
            // Cuentas
            // =========================================================

            modelBuilder.Entity<Cuentas>(entity =>
            {
                entity.ToTable("Cuentas");

                entity.HasKey(x => x.idCuenta);

                entity.Property(x => x.idCuenta)
                    .HasColumnName("idCuenta");

                entity.Property(x => x.idMesa)
                    .HasColumnName("idMesa");

                entity.Property(x => x.estado)
                    .HasColumnName("estado");

                entity.Property(x => x.fechaApertura)
                    .HasColumnName("fechaApertura");

                entity.Property(x => x.fechaCierre)
                    .HasColumnName("fechaCierre");

                entity.Property(x => x.total)
                    .HasColumnName("total")
                    .HasPrecision(10, 2);

                entity.HasOne(x => x.mesa)
                    .WithMany(x => x.cuentas)
                    .HasForeignKey(x => x.idMesa)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // MetodosPago
            // =========================================================

            modelBuilder.Entity<MetodosPago>(entity =>
            {
                entity.ToTable("MetodosPago");

                entity.HasKey(x => x.idMetodo);

                entity.Property(x => x.idMetodo)
                    .HasColumnName("idMetodo");

                entity.Property(x => x.nombreMetodo)
                    .HasColumnName("nombreMetodo");
            });


            // =========================================================
            // Comprobantes
            // =========================================================

            modelBuilder.Entity<Comprobantes>(entity =>
            {
                entity.ToTable("Comprobantes");

                entity.HasKey(x => x.idComprobante);

                entity.Property(x => x.idComprobante)
                    .HasColumnName("idComprobante");

                entity.Property(x => x.idCuenta)
                    .HasColumnName("idCuenta");

                entity.Property(x => x.fecha)
                    .HasColumnName("fecha");

                entity.Property(x => x.total)
                    .HasColumnName("total")
                    .HasPrecision(10, 2);

                entity.Property(x => x.idMetodo)
                    .HasColumnName("idMetodo");

                entity.HasOne(x => x.cuenta)
                    .WithMany(x => x.comprobantes)
                    .HasForeignKey(x => x.idCuenta)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.metodoPago)
                    .WithMany(x => x.comprobantes)
                    .HasForeignKey(x => x.idMetodo)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // DetalleCompras
            // =========================================================

            modelBuilder.Entity<DetalleCompras>(entity =>
            {
                entity.ToTable("DetalleCompras");

                entity.HasKey(x => x.idDetalleCompra);

                entity.Property(x => x.idDetalleCompra)
                    .HasColumnName("idDetalleCompra");

                entity.Property(x => x.idCompra)
                    .HasColumnName("idCompra");

                entity.Property(x => x.idProducto)
                    .HasColumnName("idProducto");

                entity.Property(x => x.cantidad)
                    .HasColumnName("cantidad");

                entity.Property(x => x.precioCompra)
                    .HasColumnName("precioCompra")
                    .HasPrecision(10, 2);

                entity.HasOne(x => x.compra)
                    .WithMany(x => x.detalles)
                    .HasForeignKey(x => x.idCompra)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.producto)
                    .WithMany(x => x.detallesCompras)
                    .HasForeignKey(x => x.idProducto)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Pedidos
            // =========================================================

            modelBuilder.Entity<Pedidos>(entity =>
            {
                entity.ToTable("Pedidos");

                entity.HasKey(x => x.idPedido);

                entity.Property(x => x.idPedido)
                    .HasColumnName("idPedido");

                entity.Property(x => x.idCuenta)
                    .HasColumnName("idCuenta");

                entity.Property(x => x.idUsuario)
                    .HasColumnName("idUsuario");

                entity.Property(x => x.fecha)
                    .HasColumnName("fecha");

                entity.Property(x => x.estadoPedido)
                    .HasColumnName("estadoPedido");

                entity.HasOne(x => x.cuenta)
                    .WithMany(x => x.pedidos)
                    .HasForeignKey(x => x.idCuenta)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.usuario)
                    .WithMany(x => x.pedidos)
                    .HasForeignKey(x => x.idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // DetallePedidos
            // =========================================================

            modelBuilder.Entity<DetallePedidos>(entity =>
            {
                entity.ToTable("DetallePedidos");

                entity.HasKey(x => x.idDetalle);

                entity.Property(x => x.idDetalle)
                    .HasColumnName("idDetalle");

                entity.Property(x => x.idPedido)
                    .HasColumnName("idPedido");

                entity.Property(x => x.idProducto)
                    .HasColumnName("idProducto");

                entity.Property(x => x.cantidad)
                    .HasColumnName("cantidad");

                entity.Property(x => x.precioUnitario)
                    .HasColumnName("precioUnitario")
                    .HasPrecision(10, 2);

                entity.HasOne(x => x.pedido)
                    .WithMany(x => x.detalles)
                    .HasForeignKey(x => x.idPedido)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.producto)
                    .WithMany(x => x.detallesPedidos)
                    .HasForeignKey(x => x.idProducto)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Productos
            // =========================================================

            modelBuilder.Entity<Productos>(entity =>
            {
                entity.ToTable("Productos");

                entity.HasKey(x => x.idProducto);

                entity.Property(x => x.idProducto)
                    .HasColumnName("idProducto");

                entity.Property(x => x.nombreProducto)
                    .HasColumnName("nombreProducto");

                entity.Property(x => x.precioVenta)
                    .HasColumnName("precioVenta")
                    .HasPrecision(10, 2);

                entity.Property(x => x.cantidadMinima)
                    .HasColumnName("cantidadMinima");

                entity.Property(x => x.estado)
                    .HasColumnName("estado");

                entity.Property(x => x.idCategoria)
                    .HasColumnName("idCategoria");

                entity.HasOne(x => x.categoria)
                    .WithMany(x => x.productos)
                    .HasForeignKey(x => x.idCategoria)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Kardex
            // =========================================================

            modelBuilder.Entity<Kardex>(entity =>
            {
                entity.ToTable("Kardex");

                entity.HasKey(x => x.idMovimiento);

                entity.Property(x => x.idMovimiento)
                    .HasColumnName("idMovimiento");

                entity.Property(x => x.idProducto)
                    .HasColumnName("idProducto");

                entity.Property(x => x.tipoMovimiento)
                    .HasColumnName("tipoMovimiento");

                entity.Property(x => x.cantidad)
                    .HasColumnName("cantidad");

                entity.Property(x => x.stockAnterior)
                    .HasColumnName("stockAnterior");

                entity.Property(x => x.stockNuevo)
                    .HasColumnName("stockNuevo");

                entity.Property(x => x.motivo)
                    .HasColumnName("motivo");

                entity.Property(x => x.fecha)
                    .HasColumnName("fecha");

                entity.Property(x => x.idUsuario)
                    .HasColumnName("idUsuario");

                entity.HasOne(x => x.producto)
                    .WithMany(x => x.movimientosKardex)
                    .HasForeignKey(x => x.idProducto)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.usuario)
                    .WithMany(x => x.movimientosKardex)
                    .HasForeignKey(x => x.idUsuario)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // Roles
            // =========================================================

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.ToTable("Roles");

                entity.HasKey(x => x.idRol);

                entity.Property(x => x.idRol)
                    .HasColumnName("idRol");

                entity.Property(x => x.nombreRol)
                    .HasColumnName("nombreRol");
            });


            // =========================================================
            // Permisos
            // =========================================================

            modelBuilder.Entity<Permisos>(entity =>
            {
                entity.ToTable("Permisos");

                entity.HasKey(x => x.idPermiso);

                entity.Property(x => x.idPermiso)
                    .HasColumnName("idPermiso");

                entity.Property(x => x.nombrePermiso)
                    .HasColumnName("nombrePermiso");
            });


            // =========================================================
            // RolesPermisos
            // PK COMPUESTA: idRol + idPermiso
            // =========================================================

            modelBuilder.Entity<RolesPermisos>(entity =>
            {
                entity.ToTable("RolesPermisos");

                entity.HasKey(x => new
                {
                    x.idRol,
                    x.idPermiso
                });

                entity.Property(x => x.idRol)
                    .HasColumnName("idRol");

                entity.Property(x => x.idPermiso)
                    .HasColumnName("idPermiso");

                entity.HasOne(x => x.rol)
                    .WithMany(x => x.rolesPermisos)
                    .HasForeignKey(x => x.idRol)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.permiso)
                    .WithMany(x => x.rolesPermisos)
                    .HasForeignKey(x => x.idPermiso)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // =========================================================
            // TipoDocumento
            // =========================================================

            modelBuilder.Entity<TipoDocumento>(entity =>
            {
                entity.ToTable("TipoDocumento");

                entity.HasKey(x => x.idTipoDocumento);

                entity.Property(x => x.idTipoDocumento)
                    .HasColumnName("idTipoDocumento");

                entity.Property(x => x.nombreTipo)
                    .HasColumnName("nombreTipo");
            });


            // =========================================================
            // Usuarios
            // =========================================================

            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.ToTable("Usuarios");

                entity.HasKey(x => x.idUsuario);

                entity.Property(x => x.idUsuario)
                    .HasColumnName("idUsuario");

                entity.Property(x => x.nombres)
                    .HasColumnName("nombres");

                entity.Property(x => x.apellidos)
                    .HasColumnName("apellidos");

                entity.Property(x => x.documento)
                    .HasColumnName("documento");

                entity.Property(x => x.idTipoDocumento)
                    .HasColumnName("idTipoDocumento");

                entity.Property(x => x.telefono)
                    .HasColumnName("telefono");

                entity.Property(x => x.passwordHash)
                    .HasColumnName("passwordHash");

                entity.Property(x => x.estado)
                    .HasColumnName("estado");

                entity.Property(x => x.idRol)
                    .HasColumnName("idRol");

                entity.Property(x => x.email)
                    .HasColumnName("email");

                entity.HasOne(x => x.tipoDocumento)
                    .WithMany(x => x.usuarios)
                    .HasForeignKey(x => x.idTipoDocumento)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(x => x.rol)
                    .WithMany(x => x.usuarios)
                    .HasForeignKey(x => x.idRol)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        public async Task<bool> BoolAsync()
        {
            return await SaveChangesAsync() > 0;
        }
    }
}