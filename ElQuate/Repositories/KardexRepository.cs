using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class KardexRepository : IKardexRepository
    {
        private readonly ElQuateSoftwareContext context;

        public KardexRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<Kardex>> GetKardex()
        {
            var data = await context.Kardex.ToListAsync();
            return data;
        }

        public async Task<Kardex> GetKardexById(int id)
        {
            var data = await context.Kardex.FirstOrDefaultAsync(x => x.idKardex == id);
            return data;
        }

        public async Task<bool> PostKardex(Kardex kardex)
        {
            await context.Kardex.AddAsync(kardex);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutKardex(Kardex kardex)
        {
            context.Kardex.Update(kardex);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteKardex(Kardex kardex)
        {
            context.Kardex.Remove(kardex);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
