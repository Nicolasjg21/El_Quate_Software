using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ProveedoresRepository : IProveedoresRepository
    {
        private readonly ElQuateDePattyContext context;

        public ProveedoresRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Proveedores>> GetProveedores()
        {
            var data = await context.Proveedores.ToListAsync();
            return data;
        }

        public async Task<Proveedores> GetProveedoresById(int id)
        {
            var data = await context.Proveedores.FirstOrDefaultAsync(x => x.idProveedor == id);
            return data;
        }

        public async Task<bool> PostProveedores(Proveedores proveedores)
        {
            await context.Proveedores.AddAsync(proveedores);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutProveedores(Proveedores proveedores)
        {
            context.Proveedores.Update(proveedores);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteProveedores(Proveedores proveedores)
        {
            context.Proveedores.Remove(proveedores);
            await context.BoolAsync();
            return true;
        }
    }
}