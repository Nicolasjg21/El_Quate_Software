using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class KardexRepository : IKardexRepository
    {
        private readonly ElQuateDePattyContext context;

        public KardexRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Kardex>> GetKardex()
        {
            var data = await context.Kardex.ToListAsync();
            return data;
        }

        public async Task<Kardex?> GetKardexById(int id)
        {
            var data = await context.Kardex.FirstOrDefaultAsync(x => x.idMovimiento == id);
            return data;
        }

        public async Task<bool> PostKardex(Kardex kardex)
        {
            await context.Kardex.AddAsync(kardex);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutKardex(Kardex kardex)
        {
            context.Kardex.Update(kardex);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteKardex(Kardex kardex)
        {
            context.Kardex.Remove(kardex);
            await context.BoolAsync();
            return true;
        }
    }
}