using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ElQuateDePatty.Services
{
    public sealed class PermisosRolService : IPermisosRolService
    {
        private static readonly TimeSpan duracionCache = TimeSpan.FromSeconds(60);

        private readonly IRolesPermisosRepository rolesPermisosRepository;
        private readonly IMemoryCache cache;

        public PermisosRolService(
            IRolesPermisosRepository rolesPermisosRepository,
            IMemoryCache cache)
        {
            this.rolesPermisosRepository = rolesPermisosRepository;
            this.cache = cache;
        }

        public async Task<bool> TienePermiso(int idRol, string permiso)
        {
            var permisos = await cache.GetOrCreateAsync($"permisos-rol:{idRol}", async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = duracionCache;

                var nombres = await rolesPermisosRepository.ObtenerNombresPermisosPorRol(idRol);

                return new HashSet<string>(nombres, StringComparer.OrdinalIgnoreCase);
            });

            return permisos != null && permisos.Contains(permiso);
        }
    }
}
