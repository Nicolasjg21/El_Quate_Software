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
            var data = await context.Productos.ToListAsync();
            return data;
        }

        // GET BY ID
        public async Task<Productos?> GetProductosById(int id)
        {
            var data = await context.Productos
                .FirstOrDefaultAsync(x => x.idProducto == id);

            return data;
        }

        // POST
        public async Task<bool> PostProductos(Productos productos)
        {
            await context.Productos.AddAsync(productos);
            await context.BoolAsync();

            return true;
        }

        // PUT
        public async Task<bool> PutProductos(Productos productos)
        {
            context.Productos.Update(productos);
            await context.BoolAsync();

            return true;
        }

        // DELETE
        public async Task<bool> DeleteProductos(Productos productos)
        {
            context.Productos.Remove(productos);
            await context.BoolAsync();

            return true;
        }

        // FILTROS DE PRODUCTOS
        public async Task<List<ProductoRespuestaDTO>> FiltrarProductos(
            ProductoFiltroDTO filtro)
        {
            var query = context.Productos.AsQueryable();

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
                    context.DetalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() >= filtro.costoMinimo.Value);
            }

            if (filtro.costoMaximo.HasValue)
            {
                query = query.Where(p =>
                    context.DetalleCompras
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
                    context.DetalleCompras.Any(dc =>
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
                            context.Kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) == 0);
                }
                else
                {
                    query = query.Where(p =>
                        (
                            context.Kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha)
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
                            context.Kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) <= p.cantidadMinima);
                }
                else
                {
                    query = query.Where(p =>
                        (
                            context.Kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) > p.cantidadMinima);
                }
            }

            // -----------------------------------------
            // RESPUESTA
            // -----------------------------------------
            return await query
                .Select(p => new ProductoRespuestaDTO
                {
                    idProducto = p.idProducto,

                    nombreProducto = p.nombreProducto,

                    precioVenta = p.precioVenta,

                    categoria = p.categoria != null
                        ? p.categoria.nombreCategoria
                        : null,

                    costoPromedio = context.DetalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() ?? 0,

                    stockActual = context.Kardex
                        .Where(k => k.idProducto == p.idProducto)
                        .OrderByDescending(k => k.fecha)
                        .Select(k => (int?)k.stockNuevo)
                        .FirstOrDefault() ?? 0,

                    stockBajo =
                        (
                            context.Kardex
                                .Where(k => k.idProducto == p.idProducto)
                                .OrderByDescending(k => k.fecha)
                                .Select(k => (int?)k.stockNuevo)
                                .FirstOrDefault() ?? 0
                        ) <= p.cantidadMinima,

                    proveedor = context.DetalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .OrderByDescending(dc => dc.idDetalleCompra)
                        .Select(dc => dc.compra.proveedor.nombreProveedor)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
    }
}