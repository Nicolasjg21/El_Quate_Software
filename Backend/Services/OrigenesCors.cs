namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Decide qué orígenes puede usar el Frontend para llamar a la API (CORS).
    /// Nunca devuelve "cualquier origen" (Sonar S5122).
    /// </summary>
    public static class OrigenesCors
    {
        /// <summary>Orígenes locales del Frontend (Live Server) que se usan solo en Development.</summary>
        private static readonly string[] origenesDesarrollo = ["http://localhost:5500", "http://127.0.0.1:5500"];

        /// <summary>
        /// Limpia los orígenes configurados (quita vacíos). Si no hay ninguno:
        /// en Development usa los orígenes locales; en cualquier otro entorno lanza una excepción.
        /// </summary>
        public static string[] Resolver(string[]? configurados, bool esDesarrollo)
        {
            var origenes = configurados?
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToArray() ?? [];

            if (origenes.Length > 0)
            {
                return origenes;
            }

            if (!esDesarrollo)
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins no está configurado. Defina los orígenes del Frontend " +
                    "(variable de entorno Cors__AllowedOrigins__0, etc.).");
            }

            return [.. origenesDesarrollo];
        }
    }
}