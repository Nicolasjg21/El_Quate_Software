using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ElQuateDePatty.Repositories
{
    public class UsuariosRepository : IUsuariosRepository
    {
        /// <summary>
        /// Vigencia en memoria de los datos de sesión (estado, rol y hash de contraseña).
        /// La validación del token se ejecuta en cada petición; dentro de este intervalo se
        /// reutiliza la última lectura en lugar de consultar la base de datos.
        /// </summary>
        private static readonly TimeSpan duracionCacheSesion = TimeSpan.FromSeconds(30);

        private readonly ElQuateDePattyContext context;
        private readonly IMemoryCache cache;

        public UsuariosRepository(ElQuateDePattyContext context, IMemoryCache cache)
        {
            this.context = context;
            this.cache = cache;
        }

        private static string ClaveSesion(int idUsuario) => $"sesion-usuario:{idUsuario}";

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

        /// <remarks>
        /// Toda modificación de un usuario (edición, cambio de contraseña o renovación del hash
        /// en el inicio de sesión) descarta sus datos de sesión en memoria.
        /// </remarks>
        public async Task<bool> PutUsuarios(Usuarios usuarios)
        {
            context.usuarios.Update(usuarios);
            await context.BoolAsync();
            cache.Remove(ClaveSesion(usuarios.idUsuario));
            return true;
        }

        public async Task<bool> DeleteUsuarios(Usuarios usuarios)
        {
            context.usuarios.Remove(usuarios);
            var eliminado = await context.BoolAsync();
            cache.Remove(ClaveSesion(usuarios.idUsuario));
            return eliminado;
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
            return await cache.GetOrCreateAsync(ClaveSesion(idUsuario), async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = duracionCacheSesion;

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
            });
        }
    }
}