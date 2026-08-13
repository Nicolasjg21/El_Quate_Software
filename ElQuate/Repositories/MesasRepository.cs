using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class MesasRepository : IMesasRepository
    {
        private readonly ElQuateSoftwareContext context;

        public MesasRepository(ElQuateSoftwareContext context)
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
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutMesas(Mesas mesas)
        {
            context.Mesas.Update(mesas);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMesas(Mesas mesas)
        {
            context.Mesas.Remove(mesas);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
