using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class MetodosPagoRepository : IMetodosPagoRepository
    {
        private readonly ElQuateDePattyContext context;

        public MetodosPagoRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<MetodosPago>> GetMetodosPago()
        {
            var data = await context.metodosPago.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<MetodosPago?> GetMetodosPagoById(int id)
        {
            var data = await context.metodosPago.FirstOrDefaultAsync(x => x.idMetodo == id);
            return data;
        }

        public async Task<bool> PostMetodosPago(MetodosPago metodosPago)
        {
            await context.metodosPago.AddAsync(metodosPago);
            return await context.BoolAsync();
        }

        public async Task<bool> PutMetodosPago(MetodosPago metodosPago)
        {
            context.metodosPago.Update(metodosPago);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteMetodosPago(MetodosPago metodosPago)
        {
            context.metodosPago.Remove(metodosPago);
            return await context.BoolAsync();
        }
    }
}