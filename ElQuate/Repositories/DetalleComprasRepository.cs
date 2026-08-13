using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class DetalleComprasRepository : IDetallecomprasRepository
    {
    {
        private readonly ElQuateSoftwareContext context;

        public DetalleComprasRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<DetalleCompras>> GetDetalleCompras()
        {
            var data = await context.DetalleCompras.ToListAsync();
            return data;
        }

        public async Task<DetalleCompras> GetDetalleComprasById(int id)
        {
            var data = await context.DetalleCompras.FirstOrDefaultAsync(x => x.idDetalleCompra == id);
            return data;
        }

        public async Task<bool> PostDetalleCompras(DetalleCompras detalleCompras)
        {
            await context.DetalleCompras.AddAsync(detalleCompras);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutDetalleCompras(DetalleCompras detalleCompras)
        {
            context.DetalleCompras.Update(detalleCompras);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDetalleCompras(DetalleCompras detalleCompras)
        {
            context.DetalleCompras.Remove(detalleCompras);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
