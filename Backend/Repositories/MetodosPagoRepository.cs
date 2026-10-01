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
            var data = await context.MetodosPago.ToListAsync();
            return data;
        }

        public async Task<MetodosPago?> GetMetodosPagoById(int id)
        {
            var data = await context.MetodosPago.FirstOrDefaultAsync(x => x.idMetodo == id);
            return data;
        }

        public async Task<bool> PostMetodosPago(MetodosPago metodosPago)
        {
            await context.MetodosPago.AddAsync(metodosPago);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutMetodosPago(MetodosPago metodosPago)
        {
            context.MetodosPago.Update(metodosPago);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteMetodosPago(MetodosPago metodosPago)
        {
            context.MetodosPago.Remove(metodosPago);
            await context.BoolAsync();
            return true;
        }
    }
}