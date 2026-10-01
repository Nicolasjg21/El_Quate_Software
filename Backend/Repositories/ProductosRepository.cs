using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ProductosRepository : IProductosRepository
    {
        private readonly ElQuateDePattyContext context;

        public ProductosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        // GET - Obtener todos los productos
        public async Task<List<Productos>> GetProductos()
        {
            var data = await context.productos.AsNoTracking().ToListAsync();
            return data;
        }

        // GET BY ID
        public async Task<Productos?> GetProductosById(int id)
        {
            var data = await context.productos
                .FirstOrDefaultAsync(x => x.idProducto == id);

            return data;
        }

        // POST
        public async Task<bool> PostProductos(Productos productos)
        {
            await context.productos.AddAsync(productos);
            return await context.BoolAsync();
        }

        // PUT
        public async Task<bool> PutProductos(Productos productos)
        {
            context.productos.Update(productos);
            await context.BoolAsync();

            return true;
        }

        // DELETE
        public async Task<bool> DeleteProductos(Productos productos)
        {
            context.productos.Remove(productos);
            return await context.BoolAsync();
        }

        // FILTROS DE PRODUCTOS
        public async Task<List<ProductoRespuestaDTO>> FiltrarProductos(
            ProductoFiltroDTO filtro)
        {
            var query = context.productos.AsNoTracking().AsQueryable();

            // -----------------------------------------
            // 1. FILTRO POR NOMBRE
            // -----------------------------------------
            if (!string.IsNullOrWhiteSpace(filtro.nombre))
            {
                query = query.Where(p =>
                    p.nombreProducto.Contains(filtro.nombre));
            }

            // -----------------------------------------
            // 2. FILTRO POR CATEGORÍA
            // -----------------------------------------
            if (filtro.idCategoria.HasValue)
            {
                query = query.Where(p =>
                    p.idCategoria == filtro.idCategoria.Value);
            }

            // -----------------------------------------
            // 3. FILTRO POR PRECIO DE VENTA
            // -----------------------------------------
            if (filtro.precioVentaMinimo.HasValue)
            {
                query = query.Where(p =>
                    p.precioVenta >= filtro.precioVentaMinimo.Value);
            }

            if (filtro.precioVentaMaximo.HasValue)
            {
                query = query.Where(p =>
                    p.precioVenta <= filtro.precioVentaMaximo.Value);
            }

            // -----------------------------------------
            // 4. FILTRO POR COSTO
            // Costo = promedio del precio de compra
            // -----------------------------------------
            if (filtro.costoMinimo.HasValue)
            {
                query = query.Where(p =>
                    context.detalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() >= filtro.costoMinimo.Value);
            }

            if (filtro.costoMaximo.HasValue)
            {
                query = query.Where(p =>
                    context.detalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() <= filtro.costoMaximo.Value);
            }

            // -----------------------------------------
            // 5. FILTRO POR PROVEEDOR
            // -----------------------------------------
            if (filtro.idProveedor.HasValue)
            {
                query = query.Where(p =>
                    context.detalleCompras.Any(dc =>
                        dc.idProducto == p.idProducto &&
                        dc.compra.idProveedor == filtro.idProveedor.Value));
            }

            // -----------------------------------------
            // 6. FILTRO SIN STOCK
            // Se toma el último movimiento del Kardex.
            // -----------------------------------------
            if (filtro.sinStock.HasValue)
            {
                if (filtro.sinStock.Value)
                {
                    query = query.Where(p =>
                        (
                            context.kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha).ThenByDescending(k => k.idMovimiento)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) == 0);
                }
                else
                {
                    query = query.Where(p =>
                        (
                            context.kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha).ThenByDescending(k => k.idMovimiento)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) > 0);
                }
            }

            // -----------------------------------------
            // 7. FILTRO STOCK BAJO
            // Stock actual <= cantidadMinima
            // -----------------------------------------
            if (filtro.stockBajo.HasValue)
            {
                if (filtro.stockBajo.Value)
                {
                    query = query.Where(p =>
                        (
                            context.kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha).ThenByDescending(k => k.idMovimiento)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) <= p.cantidadMinima);
                }
                else
                {
                    query = query.Where(p =>
                        (
                            context.kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha).ThenByDescending(k => k.idMovimiento)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) > p.cantidadMinima);
                }
            }

            // -----------------------------------------
            // RESPUESTA
            // -----------------------------------------
            var filas = await query
                .Select(p => new
                {
                    p.idProducto,
                    p.nombreProducto,
                    p.precioVenta,
                    p.cantidadMinima,

                    categoria = p.categoria != null
                        ? p.categoria.nombreCategoria
                        : null,

                    costoPromedio = context.detalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() ?? 0,

                    stockActual = context.kardex
                        .Where(k => k.idProducto == p.idProducto)
                        .OrderByDescending(k => k.fecha).ThenByDescending(k => k.idMovimiento)
                        .Select(k => (int?)k.stockNuevo)
                        .FirstOrDefault() ?? 0,

                    proveedor = context.detalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .OrderByDescending(dc => dc.idDetalleCompra)
                        .Select(dc => dc.compra.proveedor.nombreProveedor)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return filas
                .Select(f => new ProductoRespuestaDTO
                {
                    idProducto = f.idProducto,
                    nombreProducto = f.nombreProducto,
                    precioVenta = f.precioVenta,
                    categoria = f.categoria,
                    costoPromedio = f.costoPromedio,
                    stockActual = f.stockActual,
                    stockBajo = f.stockActual <= f.cantidadMinima,
                    proveedor = f.proveedor
                })
                .ToList();
        }
    }
}
