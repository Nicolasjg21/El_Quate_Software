using ElQuateDePatty.Context;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
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
            var data = await context.mesas.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Mesas?> GetMesasById(int id)
        {
            var data = await context.mesas.FirstOrDefaultAsync(x => x.idMesa == id);
            return data;
        }

        public async Task<List<Mesas>> GetMesasByEstado(string estado)
        {
            var data = await context.mesas
                .AsNoTracking()
                .Where(x => x.estado == estado)
                .ToListAsync();

            return data;
        }

        public async Task<bool> PostMesas(Mesas mesas)
        {
            await context.mesas.AddAsync(mesas);
            return await context.BoolAsync();
        }

        public async Task<bool> PutMesas(Mesas mesas)
        {
            context.mesas.Update(mesas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteMesas(Mesas mesas)
        {
            context.mesas.Remove(mesas);
            return await context.BoolAsync();
        }

        public async Task<List<Mesas>> FiltrarMesas(MesaFiltroDTO filtro)
        {
            var query = context.mesas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.estado))
            {
                query = query.Where(m =>
                    m.estado == filtro.estado);
            }

            return await query.ToListAsync();
        }
    }
}