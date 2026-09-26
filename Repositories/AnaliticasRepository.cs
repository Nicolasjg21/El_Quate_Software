using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class AnaliticasRepository : IAnaliticasRepository
    {
        private readonly ElQuateDePattyContext context;

        public AnaliticasRepository(
            ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<VentaAnaliticaDTO> ObtenerAnaliticasVentas(
            PeriodoFiltroDTO filtro)
        {
            var comprobantes = context.Comprobantes
                .AsQueryable();

            // -----------------------------------------
            // FILTRO POR FECHA DESDE
            // -----------------------------------------
            if (filtro.FechaDesde.HasValue)
            {
                comprobantes = comprobantes.Where(c =>
                    c.fecha >= filtro.FechaDesde.Value);
            }

            // -----------------------------------------
            // FILTRO POR FECHA HASTA
            // -----------------------------------------
            if (filtro.FechaHasta.HasValue)
            {
                comprobantes = comprobantes.Where(c =>
                    c.fecha <= filtro.FechaHasta.Value);
            }

            // -----------------------------------------
            // TOTAL DE VENTAS
            // -----------------------------------------
            var totalVentas = await comprobantes.CountAsync();

            // -----------------------------------------
            // TOTAL DE INGRESOS
            // -----------------------------------------
            var totalIngresos =
                await comprobantes
                    .Select(c => (decimal?)c.total)
                    .SumAsync() ?? 0;

            // -----------------------------------------
            // PROMEDIO DE VENTA
            // -----------------------------------------
            var promedioVenta = totalVentas > 0
                ? totalIngresos / totalVentas
                : 0;

            // -----------------------------------------
            // TOTAL DE PRODUCTOS VENDIDOS
            // -----------------------------------------
            var idsCuentas = comprobantes
                .Select(c => c.idCuenta);

            var totalProductosVendidos =
                await context.DetallePedidos
                    .Where(dp =>
                        context.Pedidos.Any(p =>
                            p.idPedido == dp.idPedido &&
                            context.Cuentas.Any(c =>
                                c.idCuenta == p.idCuenta &&
                                idsCuentas.Contains(c.idCuenta))))
                    .SumAsync(dp => (int?)dp.cantidad) ?? 0;

            return new VentaAnaliticaDTO
            {
                TotalVentas = totalVentas,
                TotalIngresos = totalIngresos,
                PromedioVenta = promedioVenta,
                TotalProductosVendidos = totalProductosVendidos
            };
        }
    }
}