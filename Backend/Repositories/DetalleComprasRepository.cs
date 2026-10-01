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
            var data = await context.detalleCompras.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<DetalleCompras?> GetDetalleComprasById(int id)
        {
            var data = await context.detalleCompras.FirstOrDefaultAsync(x => x.idDetalleCompra == id);
            return data;
        }

        public async Task<bool> PostDetalleCompras(DetalleCompras detalleCompras)
        {
            await context.detalleCompras.AddAsync(detalleCompras);
            return await context.BoolAsync();
        }

        public async Task<bool> PutDetalleCompras(DetalleCompras detalleCompras)
        {
            context.detalleCompras.Update(detalleCompras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteDetalleCompras(DetalleCompras detalleCompras)
        {
            context.detalleCompras.Remove(detalleCompras);
            return await context.BoolAsync();
        }
    }
}