using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class DetallePedidosRepository : IDetallePedidosRepository
    {
        private readonly ElQuateDePattyContext context;

        public DetallePedidosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Lista las líneas de pedido. El filtro por cuenta se resuelve en la base de datos a través
        /// del pedido de cada línea, de modo que solo viajan las líneas de la cuenta solicitada.
        /// </summary>
        public async Task<List<DetallePedidos>> GetDetallePedidos(int? idCuenta = null)
        {
            var consulta = context.detallePedidos.AsNoTracking();

            if (idCuenta.HasValue)
            {
                var cuenta = idCuenta.Value;
                consulta = consulta.Where(d => d.pedido.idCuenta == cuenta);
            }

            return await consulta.ToListAsync();
        }

        public async Task<DetallePedidos?> GetDetallePedidosById(int id)
        {
            var data = await context.detallePedidos.FirstOrDefaultAsync(x => x.idDetalle == id);
            return data;
        }

        public async Task<bool> PostDetallePedidos(DetallePedidos detallePedidos)
        {
            await context.detallePedidos.AddAsync(detallePedidos);
            return await context.BoolAsync();
        }

        public async Task<bool> PutDetallePedidos(DetallePedidos detallePedidos)
        {
            context.detallePedidos.Update(detallePedidos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteDetallePedidos(DetallePedidos detallePedidos)
        {
            context.detallePedidos.Remove(detallePedidos);
            return await context.BoolAsync();
        }
    }
}