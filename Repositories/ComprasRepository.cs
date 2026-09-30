using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Compras;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ComprasRepository : IComprasRepository
    {
        private readonly ElQuateDePattyContext context;

        public ComprasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Compras>> GetCompras()
        {
            var data = await context.Compras.ToListAsync();
            return data;
        }

        public async Task<Compras?> GetComprasById(int id)
        {
            var data = await context.Compras.FirstOrDefaultAsync(x => x.idCompra == id);
            return data;
        }

        public async Task<bool> PostCompras(Compras compras)
        {
            await context.Compras.AddAsync(compras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutCompras(Compras compras)
        {
            context.Compras.Update(compras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCompras(Compras compras)
        {
            context.Compras.Remove(compras);
            await context.BoolAsync();
            return true;
        }

        public async Task<List<Compras>> FiltrarCompras(CompraFiltroDTO filtro)
        {
            var query = context.Compras.AsQueryable();

            // -----------------------------------------
            // 1. FILTRO POR FECHA DESDE
            // -----------------------------------------
            if (filtro.fechaDesde.HasValue)
            {
                query = query.Where(c =>
                    c.fecha >= filtro.fechaDesde.Value);
            }

            // -----------------------------------------
            // 2. FILTRO POR FECHA HASTA
            // -----------------------------------------
            if (filtro.fechaHasta.HasValue)
            {
                query = query.Where(c =>
                    c.fecha <= filtro.fechaHasta.Value);
            }

            // -----------------------------------------
            // 3. FILTRO POR PROVEEDOR
            // -----------------------------------------
            if (filtro.idProveedor.HasValue)
            {
                query = query.Where(c =>
                    c.idProveedor == filtro.idProveedor.Value);
            }

            // -----------------------------------------
            // 4. FILTRO POR TOTAL DE ARTÍCULOS
            // -----------------------------------------
            if (filtro.totalArticulosMinimo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (int?)dc.cantidad) >=
                        filtro.totalArticulosMinimo.Value);
            }

            if (filtro.totalArticulosMaximo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (int?)dc.cantidad) <=
                        filtro.totalArticulosMaximo.Value);
            }

            // -----------------------------------------
            // 5. FILTRO POR COSTO TOTAL
            // cantidad × precio de compra
            // -----------------------------------------
            if (filtro.costoTotalMinimo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (decimal?)(dc.cantidad * dc.precioCompra)) >=
                        filtro.costoTotalMinimo.Value);
            }

            if (filtro.costoTotalMaximo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (decimal?)(dc.cantidad * dc.precioCompra)) <=
                        filtro.costoTotalMaximo.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Compras>> FiltrarHistorialCompras(PeriodoFiltroDTO filtro)
        {
            var query = context.Compras.AsQueryable();

            if (filtro.fechaDesde.HasValue)
            {
                query = query.Where(c =>
                    c.fecha >= filtro.fechaDesde.Value);
            }

            if (filtro.fechaHasta.HasValue)
            {
                query = query.Where(c =>
                    c.fecha <= filtro.fechaHasta.Value);
            }

            return await query
                .OrderByDescending(c => c.fecha)
                .ToListAsync();
        }
    }
}