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
            var data = await context.Auditorias.ToListAsync();
            return data;
        }

        public async Task<Auditorias?> GetAuditoriasById(int id)
        {
            var data = await context.Auditorias.FirstOrDefaultAsync(x => x.idAuditoria == id);
            return data;
        }

        public async Task<bool> PostAuditorias(Auditorias auditorias)
        {
            await context.Auditorias.AddAsync(auditorias);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutAuditorias(Auditorias auditorias)
        {
            context.Auditorias.Update(auditorias);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteAuditorias(Auditorias auditorias)
        {
            context.Auditorias.Remove(auditorias);
            await context.BoolAsync();
            return true;
        }
    }
}