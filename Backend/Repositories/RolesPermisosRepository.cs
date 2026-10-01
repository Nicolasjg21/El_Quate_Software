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
            var data = await context.rolesPermisos.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<RolesPermisos?> GetRolesPermisosById(int idRol, int idPermiso)
        {
            var data = await context.rolesPermisos
                .FirstOrDefaultAsync(x => x.idRol == idRol && x.idPermiso == idPermiso);

            return data;
        }

        public async Task<bool> PostRolesPermisos(RolesPermisos rolesPermisos)
        {
            await context.rolesPermisos.AddAsync(rolesPermisos);
            return await context.BoolAsync();
        }

        public async Task<bool> PutRolesPermisos(RolesPermisos rolesPermisos)
        {
            context.rolesPermisos.Update(rolesPermisos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteRolesPermisos(RolesPermisos rolesPermisos)
        {
            context.rolesPermisos.Remove(rolesPermisos);
            return await context.BoolAsync();
        }

        public async Task<List<string>> ObtenerNombresPermisosPorRol(int idRol)
        {
            return await context.rolesPermisos
                .AsNoTracking()
                .Where(rp => rp.idRol == idRol)
                .Select(rp => rp.permiso.nombrePermiso)
                .ToListAsync();
        }
    }
}
