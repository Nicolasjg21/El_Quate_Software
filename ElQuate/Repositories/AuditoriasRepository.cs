using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class AuditoriasRepository : IAuditoriasRepository
    {
        private readonly ElQuateSoftwareContext context;

        public AuditoriasRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<Auditorias>> GetAuditorias()
        {
            var data = await context.Auditorias.ToListAsync();
            return data;
        }

        public async Task<Auditorias> GetAuditoriasById(int id)
        {
            var data = await context.Auditorias.FirstOrDefaultAsync(x => x.idAuditoria == id);
            return data;
        }

        public async Task<bool> PostAuditorias(Auditorias auditorias)
        {
            await context.Auditorias.AddAsync(auditorias);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutAuditorias(Auditorias auditorias)
        {
            context.Auditorias.Update(auditorias);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAuditorias(Auditorias auditorias)
        {
            context.Auditorias.Remove(auditorias);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
