using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class PedidosRepository : IPedidosRepository
    {
        private readonly ElQuateDePattyContext context;

        public PedidosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Pedidos>> GetPedidos()
        {
            var data = await context.Pedidos.ToListAsync();
            return data;
        }

        public async Task<Pedidos?> GetPedidosById(int id)
        {
            var data = await context.Pedidos.FirstOrDefaultAsync(x => x.idPedido == id);
            return data;
        }

        public async Task<bool> PostPedidos(Pedidos pedidos)
        {
            await context.Pedidos.AddAsync(pedidos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutPedidos(Pedidos pedidos)
        {
            context.Pedidos.Update(pedidos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeletePedidos(Pedidos pedidos)
        {
            context.Pedidos.Remove(pedidos);
            await context.BoolAsync();
            return true;
        }

        public async Task<List<Pedidos>> FiltrarPedidos(PedidoFiltroDTO filtro)
        {
            var query = context.Pedidos
                .Include(p => p.cuenta)
                    .ThenInclude(c => c.mesa)
                .Include(p => p.usuario)
                .Include(p => p.cuenta)
                    .ThenInclude(c => c.comprobantes)
                        .ThenInclude(c => c.metodoPago)
                .AsQueryable();

            // Mesa
            if (filtro.NumeroMesa.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.mesa.numeroMesa == filtro.NumeroMesa.Value);
            }

            // Usuario
            if (filtro.IdUsuario.HasValue)
            {
                query = query.Where(p =>
                    p.idUsuario == filtro.IdUsuario.Value);
            }

            // Fecha
            if (filtro.Fecha.HasValue)
            {
                var fecha = filtro.Fecha.Value.Date;

                query = query.Where(p =>
                    p.fecha.Date == fecha);
            }

            // Hora Inicio
            if (filtro.HoraInicio.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay >= filtro.HoraInicio.Value);
            }

            // Hora Fin
            if (filtro.HoraFin.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay <= filtro.HoraFin.Value);
            }

            // Total mínimo
            if (filtro.TotalMinimo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total >= filtro.TotalMinimo.Value);
            }

            // Total máximo
            if (filtro.TotalMaximo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total <= filtro.TotalMaximo.Value);
            }

            // Método de pago
            if (filtro.IdMetodoPago.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.comprobantes.Any(c =>
                        c.idMetodo == filtro.IdMetodoPago.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<List<Pedidos>> FiltrarHistorialPedidos(PeriodoFiltroDTO filtro)
        {
            var query = context.Pedidos.AsQueryable();

            if (filtro.FechaDesde.HasValue)
            {
                query = query.Where(p =>
                    p.fecha >= filtro.FechaDesde.Value);
            }

            if (filtro.FechaHasta.HasValue)
            {
                query = query.Where(p =>
                    p.fecha <= filtro.FechaHasta.Value);
            }

            return await query
                .OrderByDescending(p => p.fecha)
                .ToListAsync();
        }
    }
}
