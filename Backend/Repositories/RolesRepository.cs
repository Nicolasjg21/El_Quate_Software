using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class RolesRepository : IRolesRepository
    {
        private readonly ElQuateDePattyContext context;

        public RolesRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Roles>> GetRoles()
        {
            var data = await context.roles.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Roles?> GetRolesById(int id)
        {
            var data = await context.roles.FirstOrDefaultAsync(x => x.idRol == id);
            return data;
        }

        public async Task<bool> PostRoles(Roles roles)
        {
            await context.roles.AddAsync(roles);
            return await context.BoolAsync();
        }

        public async Task<bool> PutRoles(Roles roles)
        {
            context.roles.Update(roles);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteRoles(Roles roles)
        {
            context.roles.Remove(roles);
            return await context.BoolAsync();
        }
    }
}