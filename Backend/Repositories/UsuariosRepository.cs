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
            var data = await context.usuarios.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Usuarios?> GetUsuariosById(int id)
        {
            var data = await context.usuarios.FirstOrDefaultAsync(x => x.idUsuario == id);
            return data;
        }

        public async Task<bool> PostUsuarios(Usuarios usuarios)
        {
            await context.usuarios.AddAsync(usuarios);
            return await context.BoolAsync();
        }

        public async Task<bool> PutUsuarios(Usuarios usuarios)
        {
            context.usuarios.Update(usuarios);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteUsuarios(Usuarios usuarios)
        {
            context.usuarios.Remove(usuarios);
            return await context.BoolAsync();
        }

        public async Task<Usuarios?> GetUsuariosByEmail(string email)
        {
            return await context.usuarios
                .FirstOrDefaultAsync(u => u.email == email);
        }

        public async Task<bool> ExisteEmail(string email, int? idUsuarioExcluido = null)
        {
            return await context.usuarios
                .AsNoTracking()
                .AnyAsync(u => u.email == email &&
                    (idUsuarioExcluido == null || u.idUsuario != idUsuarioExcluido));
        }

        public async Task<Usuarios?> ObtenerUsuarioActivoParaSesion(int idUsuario)
        {
            return await context.usuarios
                .AsNoTracking()
                .Where(u => u.idUsuario == idUsuario && u.estado)
                .Select(u => new Usuarios
                {
                    idUsuario = u.idUsuario,
                    idRol = u.idRol,
                    passwordHash = u.passwordHash
                })
                .FirstOrDefaultAsync();
        }
    }
}