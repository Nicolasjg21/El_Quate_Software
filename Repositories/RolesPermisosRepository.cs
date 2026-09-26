using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class RolesPermisosRepository : IRolesPermisosRepository
    {
        private readonly ElQuateDePattyContext context;

        public RolesPermisosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<RolesPermisos>> GetRolesPermisos()
        {
            var data = await context.RolesPermisos.ToListAsync();
            return data;
        }

        public async Task<RolesPermisos?> GetRolesPermisosById(int idRol, int idPermiso)
        {
            var data = await context.RolesPermisos
                .FirstOrDefaultAsync(x => x.idRol == idRol && x.idPermiso == idPermiso);

            return data;
        }

        public async Task<bool> PostRolesPermisos(RolesPermisos rolesPermisos)
        {
            await context.RolesPermisos.AddAsync(rolesPermisos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutRolesPermisos(RolesPermisos rolesPermisos)
        {
            context.RolesPermisos.Update(rolesPermisos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteRolesPermisos(RolesPermisos rolesPermisos)
        {
            context.RolesPermisos.Remove(rolesPermisos);
            await context.BoolAsync();
            return true;
        }
    }
}