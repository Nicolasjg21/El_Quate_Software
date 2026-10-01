using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class DetalleComprasRepository : IDetalleComprasRepository
    {
        private readonly ElQuateDePattyContext context;

        public DetalleComprasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<DetalleCompras>> GetDetalleCompras()
        {
            var data = await context.DetalleCompras.ToListAsync();
            return data;
        }

        public async Task<DetalleCompras?> GetDetalleComprasById(int id)
        {
            var data = await context.DetalleCompras.FirstOrDefaultAsync(x => x.idDetalleCompra == id);
            return data;
        }

        public async Task<bool> PostDetalleCompras(DetalleCompras detalleCompras)
        {
            await context.DetalleCompras.AddAsync(detalleCompras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutDetalleCompras(DetalleCompras detalleCompras)
        {
            context.DetalleCompras.Update(detalleCompras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteDetalleCompras(DetalleCompras detalleCompras)
        {
            context.DetalleCompras.Remove(detalleCompras);
            await context.BoolAsync();
            return true;
        }
    }
}