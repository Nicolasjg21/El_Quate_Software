using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class UsuariosRepository : IUsuariosRepository
    {
        private readonly ElQuateDePattyContext context;

        public UsuariosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Usuarios>> GetUsuarios()
        {
            var data = await context.Usuarios.ToListAsync();
            return data;
        }

        public async Task<Usuarios?> GetUsuariosById(int id)
        {
            var data = await context.Usuarios.FirstOrDefaultAsync(x => x.idUsuario == id);
            return data;
        }

        public async Task<bool> PostUsuarios(Usuarios usuarios)
        {
            await context.Usuarios.AddAsync(usuarios);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutUsuarios(Usuarios usuarios)
        {
            context.Usuarios.Update(usuarios);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteUsuarios(Usuarios usuarios)
        {
            context.Usuarios.Remove(usuarios);
            await context.BoolAsync();
            return true;
        }
    }
}