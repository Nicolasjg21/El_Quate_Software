using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class AuditoriasRepository : IAuditoriasRepository
    {
        private readonly ElQuateDePattyContext context;

        public AuditoriasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Auditorias>> GetAuditorias()
        {
            var data = await context.auditorias.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Auditorias?> GetAuditoriasById(int id)
        {
            var data = await context.auditorias.FirstOrDefaultAsync(x => x.idAuditoria == id);
            return data;
        }

        public async Task<bool> PostAuditorias(Auditorias auditorias)
        {
            await context.auditorias.AddAsync(auditorias);
            return await context.BoolAsync();
        }

        public async Task<bool> PutAuditorias(Auditorias auditorias)
        {
            context.auditorias.Update(auditorias);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteAuditorias(Auditorias auditorias)
        {
            context.auditorias.Remove(auditorias);
            return await context.BoolAsync();
        }
    }
}