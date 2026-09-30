using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Pedidos;
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
            if (filtro.numeroMesa.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.mesa.numeroMesa == filtro.numeroMesa.Value);
            }

            // Usuario
            if (filtro.idUsuario.HasValue)
            {
                query = query.Where(p =>
                    p.idUsuario == filtro.idUsuario.Value);
            }

            // Fecha
            if (filtro.fecha.HasValue)
            {
                var fecha = filtro.fecha.Value.Date;

                query = query.Where(p =>
                    p.fecha.Date == fecha);
            }

            // Hora Inicio
            if (filtro.horaInicio.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay >= filtro.horaInicio.Value);
            }

            // Hora Fin
            if (filtro.horaFin.HasValue)
            {
                query = query.Where(p =>
                    p.fecha.TimeOfDay <= filtro.horaFin.Value);
            }

            // Total mínimo
            if (filtro.totalMinimo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total >= filtro.totalMinimo.Value);
            }

            // Total máximo
            if (filtro.totalMaximo.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.total <= filtro.totalMaximo.Value);
            }

            // Método de pago
            if (filtro.idMetodoPago.HasValue)
            {
                query = query.Where(p =>
                    p.cuenta.comprobantes.Any(c =>
                        c.idMetodo == filtro.idMetodoPago.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<List<Pedidos>> FiltrarHistorialPedidos(PeriodoFiltroDTO filtro)
        {
            var query = context.Pedidos.AsQueryable();

            if (filtro.fechaDesde.HasValue)
            {
                query = query.Where(p =>
                    p.fecha >= filtro.fechaDesde.Value);
            }

            if (filtro.fechaHasta.HasValue)
            {
                query = query.Where(p =>
                    p.fecha <= filtro.fechaHasta.Value);
            }

            return await query
                .OrderByDescending(p => p.fecha)
                .ToListAsync();
        }
    }
}
