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

        public async Task<List<DetallePedidos>> GetDetallePedidos()
        {
            var data = await context.detallePedidos.AsNoTracking().ToListAsync();
            return data;
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