using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class MesasRepository : IMesasRepository
    {
        private readonly ElQuateDePattyContext context;

        public MesasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Mesas>> GetMesas()
        {
            var data = await context.Mesas.ToListAsync();
            return data;
        }

        public async Task<Mesas> GetMesasById(int id)
        {
            var data = await context.Mesas.FirstOrDefaultAsync(x => x.idMesa == id);
            return data;
        }

        public async Task<bool> PostMesas(Mesas mesas)
        {
            await context.Mesas.AddAsync(mesas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutMesas(Mesas mesas)
        {
            context.Mesas.Update(mesas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteMesas(Mesas mesas)
        {
            context.Mesas.Remove(mesas);
            await context.BoolAsync();
            return true;
        }
    }
}