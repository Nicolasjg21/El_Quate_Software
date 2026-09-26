using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs;
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
        public async Task<Productos> GetProductosById(int id)
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
        public async Task<List<Productos>> FiltrarProductos(
            ProductoFiltroDTO filtro)
        {
            var query = context.Productos.AsQueryable();

            // -----------------------------------------
            // 1. FILTRO POR NOMBRE
            // -----------------------------------------
            if (!string.IsNullOrWhiteSpace(filtro.Nombre))
            {
                query = query.Where(p =>
                    p.nombreProducto.Contains(filtro.Nombre));
            }

            // -----------------------------------------
            // 2. FILTRO POR CATEGORÍA
            // -----------------------------------------
            if (filtro.IdCategoria.HasValue)
            {
                query = query.Where(p =>
                    p.idCategoria == filtro.IdCategoria.Value);
            }

            // -----------------------------------------
            // 3. FILTRO POR PRECIO DE VENTA
            // -----------------------------------------
            if (filtro.PrecioVentaMinimo.HasValue)
            {
                query = query.Where(p =>
                    p.precioVenta >= filtro.PrecioVentaMinimo.Value);
            }

            if (filtro.PrecioVentaMaximo.HasValue)
            {
                query = query.Where(p =>
                    p.precioVenta <= filtro.PrecioVentaMaximo.Value);
            }

            // -----------------------------------------
            // 4. FILTRO POR COSTO
            // Costo = promedio del precio de compra
            // registrado en DetalleCompras
            // -----------------------------------------
            if (filtro.CostoMinimo.HasValue)
            {
                query = query.Where(p =>
                    context.DetalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() >= filtro.CostoMinimo.Value);
            }

            if (filtro.CostoMaximo.HasValue)
            {
                query = query.Where(p =>
                    context.DetalleCompras
                        .Where(dc => dc.idProducto == p.idProducto)
                        .Select(dc => (decimal?)dc.precioCompra)
                        .Average() <= filtro.CostoMaximo.Value);
            }

            // -----------------------------------------
            // 5. FILTRO POR PROVEEDOR
            // Productos que hayan sido comprados
            // a un proveedor determinado
            // -----------------------------------------
            if (filtro.IdProveedor.HasValue)
            {
                query = query.Where(p =>
                    context.DetalleCompras.Any(dc =>
                        dc.idProducto == p.idProducto &&
                        context.Compras.Any(c =>
                            c.idCompra == dc.idCompra &&
                            c.idProveedor == filtro.IdProveedor.Value)));
            }

            // -----------------------------------------
            // 6. FILTRO SIN STOCK
            // Stock actual = 0
            // Se obtiene desde el último movimiento
            // registrado en Kardex.
            // -----------------------------------------
            if (filtro.SinStock.HasValue)
            {
                if (filtro.SinStock.Value)
                {
                    query = query.Where(p =>
                        !context.Kardex.Any(k =>
                            k.idProducto == p.idProducto &&
                            k.stockNuevo > 0));
                }
                else
                {
                    query = query.Where(p =>
                        context.Kardex.Any(k =>
                            k.idProducto == p.idProducto &&
                            k.stockNuevo > 0));
                }
            }

            // -----------------------------------------
            // 7. FILTRO STOCK BAJO
            // Stock actual <= cantidadMinima
            // -----------------------------------------
            if (filtro.StockBajo.HasValue)
            {
                if (filtro.StockBajo.Value)
                {
                    query = query.Where(p =>
                        context.Kardex
                            .Where(k => k.idProducto == p.idProducto)
                            .OrderByDescending(k => k.fecha)
                            .Select(k => (int?)k.stockNuevo)
                            .FirstOrDefault() <= p.cantidadMinima);
                }
                else
                {
                    query = query.Where(p =>
                        context.Kardex
                            .Where(k => k.idProducto == p.idProducto)
                            .OrderByDescending(k => k.fecha)
                            .Select(k => (int?)k.stockNuevo)
                            .FirstOrDefault() > p.cantidadMinima);
                }
            }

            return await query.ToListAsync();
        }
    }
}