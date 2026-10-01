using Microsoft.Extensions.Caching.Memory;

namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Lista de sesiones revocadas (logout) en memoria del proceso.
    /// Con más de una instancia debe sustituirse por una caché distribuida.
    /// </summary>
    public sealed class RevocacionTokenService : IRevocacionTokenService
    {
        private readonly IMemoryCache cache;

        public RevocacionTokenService(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public void Revocar(string idSesion, DateTimeOffset expiraEn)
        {
            if (expiraEn <= DateTimeOffset.UtcNow)
            {
                return;
            }

            cache.Set(Clave(idSesion), true, expiraEn);
        }

        public bool EstaRevocada(string idSesion)
        {
            return cache.TryGetValue(Clave(idSesion), out _);
        }

        private static string Clave(string idSesion)
        {
            return $"sesion-revocada:{idSesion}";
        }
    }
}
