using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Repositories.Interfaces;
using ElQuateSoftwareSoftware.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateSoftware.Repositories
{
    public class PedidosRepository : IPedidosRepository
    {
        private readonly ElQuateSoftwareContext context;

        public PedidosRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<Pedidos>> GetPedidos()
        {
            var data = await context.Pedidos.ToListAsync();
            return data;
        }

        public async Task<Pedidos> GetPedidosById(int id)
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
    }
}
