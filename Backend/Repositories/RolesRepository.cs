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
            var data = await context.Roles.ToListAsync();
            return data;
        }

        public async Task<Roles?> GetRolesById(int id)
        {
            var data = await context.Roles.FirstOrDefaultAsync(x => x.idRol == id);
            return data;
        }

        public async Task<bool> PostRoles(Roles roles)
        {
            await context.Roles.AddAsync(roles);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutRoles(Roles roles)
        {
            context.Roles.Update(roles);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteRoles(Roles roles)
        {
            context.Roles.Remove(roles);
            await context.BoolAsync();
            return true;
        }
    }
}