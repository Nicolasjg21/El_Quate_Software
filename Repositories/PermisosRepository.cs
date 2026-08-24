using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class PermisosRepository : IPermisosRepository
    {
        private readonly ElQuateDePattyContext context;

        public PermisosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Permisos>> GetPermisos()
        {
            var data = await context.Permisos.ToListAsync();
            return data;
        }

        public async Task<Permisos> GetPermisosById(int id)
        {
            var data = await context.Permisos.FirstOrDefaultAsync(x => x.idPermiso == id);
            return data;
        }

        public async Task<bool> PostPermisos(Permisos permisos)
        {
            await context.Permisos.AddAsync(permisos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutPermisos(Permisos permisos)
        {
            context.Permisos.Update(permisos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeletePermisos(Permisos permisos)
        {
            context.Permisos.Remove(permisos);
            await context.BoolAsync();
            return true;
        }
    }
}