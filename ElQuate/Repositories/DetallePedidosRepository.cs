using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class DetallePedidosRepository : IDetallePedidosRepository
    {
        private readonly ElQuateSoftwareContext context;

        public DetallePedidosRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<DetallePedidos>> GetDetallePedidos()
        {
            var data = await context.DetallePedidos.ToListAsync();
            return data;
        }

        public async Task<DetallePedidos> GetDetallePedidosById(int id)
        {
            var data = await context.DetallePedidos.FirstOrDefaultAsync(x => x.idDetalle == id);
            return data;
        }

        public async Task<bool> PostDetallePedidos(DetallePedidos detallePedidos)
        {
            await context.DetallePedidos.AddAsync(detallePedidos);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutDetallePedidos(DetallePedidos detallePedidos)
        {
            context.DetallePedidos.Update(detallePedidos);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDetallePedidos(DetallePedidos detallePedidos)
        {
            context.DetallePedidos.Remove(detallePedidos);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
