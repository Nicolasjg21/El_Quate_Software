using Microsoft.Extensions.Caching.Memory;

namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Bloqueo temporal por correo tras varios fallos consecutivos de login.
    /// Se aplica igual a correos existentes e inexistentes (no permite enumerar usuarios).
    /// Memoria del proceso: con varias instancias debe usar una caché distribuida.
    /// </summary>
    public sealed class RastreadorIntentosLogin : IRastreadorIntentosLogin
    {
        private const int maximoFallos = 5;
        private static readonly TimeSpan ventana = TimeSpan.FromMinutes(15);

        private readonly IMemoryCache cache;

        public RastreadorIntentosLogin(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public bool EstaBloqueado(string email)
        {
            return cache.TryGetValue(Clave(email), out Contador? contador)
                && contador != null
                && contador.fallos >= maximoFallos;
        }

        public void RegistrarFallo(string email)
        {
            var clave = Clave(email);

            var contador = cache.GetOrCreate(clave, entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = ventana;
                return new Contador();
            });

            if (contador != null)
            {
                Interlocked.Increment(ref contador.fallos);
            }
        }

        public void Limpiar(string email)
        {
            cache.Remove(Clave(email));
        }

        private static string Clave(string email)
        {
            var normalizado = email.Trim().ToLowerInvariant();

            if (normalizado.Length > 254)
            {
                normalizado = normalizado.Substring(0, 254);
            }

            return $"login-fallos:{normalizado}";
        }

        private sealed class Contador
        {
            public int fallos;
        }
    }
}
