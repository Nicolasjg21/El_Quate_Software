using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs;
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
            if (filtro.FechaDesde.HasValue)
            {
                query = query.Where(c =>
                    c.fecha >= filtro.FechaDesde.Value);
            }

            // -----------------------------------------
            // 2. FILTRO POR FECHA HASTA
            // -----------------------------------------
            if (filtro.FechaHasta.HasValue)
            {
                query = query.Where(c =>
                    c.fecha <= filtro.FechaHasta.Value);
            }

            // -----------------------------------------
            // 3. FILTRO POR PROVEEDOR
            // -----------------------------------------
            if (filtro.IdProveedor.HasValue)
            {
                query = query.Where(c =>
                    c.idProveedor == filtro.IdProveedor.Value);
            }

            // -----------------------------------------
            // 4. FILTRO POR TOTAL DE ARTÍCULOS
            // -----------------------------------------
            if (filtro.TotalArticulosMinimo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (int?)dc.cantidad) >=
                        filtro.TotalArticulosMinimo.Value);
            }

            if (filtro.TotalArticulosMaximo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (int?)dc.cantidad) <=
                        filtro.TotalArticulosMaximo.Value);
            }

            // -----------------------------------------
            // 5. FILTRO POR COSTO TOTAL
            // cantidad × precio de compra
            // -----------------------------------------
            if (filtro.CostoTotalMinimo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (decimal?)(dc.cantidad * dc.precioCompra)) >=
                        filtro.CostoTotalMinimo.Value);
            }

            if (filtro.CostoTotalMaximo.HasValue)
            {
                query = query.Where(c =>
                    context.DetalleCompras
                        .Where(dc => dc.idCompra == c.idCompra)
                        .Sum(dc => (decimal?)(dc.cantidad * dc.precioCompra)) <=
                        filtro.CostoTotalMaximo.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Compras>> FiltrarHistorialCompras(PeriodoFiltroDTO filtro)
        {
            var query = context.Compras.AsQueryable();

            if (filtro.FechaDesde.HasValue)
            {
                query = query.Where(c =>
                    c.fecha >= filtro.FechaDesde.Value);
            }

            if (filtro.FechaHasta.HasValue)
            {
                query = query.Where(c =>
                    c.fecha <= filtro.FechaHasta.Value);
            }

            return await query
                .OrderByDescending(c => c.fecha)
                .ToListAsync();
        }
    }
}