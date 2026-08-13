using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class ComprasRepository : IComprasReposiroty
    {
        private readonly ElQuateSoftwareContext context;

        public ComprasRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<Compras>> GetCompras()
        {
            var data = await context.Compras.ToListAsync();
            return data;
        }

        public async Task<Compras> GetComprasById(int id)
        {
            var data = await context.Compras.FirstOrDefaultAsync(x => x.idCompra == id);
            return data;
        }

        public async Task<bool> PostCompras(Compras compras)
        {
            await context.Compras.AddAsync(compras);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutCompras(Compras compras)
        {
            context.Compras.Update(compras);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCompras(Compras compras)
        {
            context.Compras.Remove(compras);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
