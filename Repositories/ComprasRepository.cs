using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ComprasRepository : IComprasRepository
    {
        private readonly ElQuateDePattyContext context;

        public ComprasRepository(ElQuateDePattyContext context)
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
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutCompras(Compras compras)
        {
            context.Compras.Update(compras);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCompras(Compras compras)
        {
            context.Compras.Remove(compras);
            await context.BoolAsync();
            return true;
        }
    }
}