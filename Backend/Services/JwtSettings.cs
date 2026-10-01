using System.Text;

namespace ElQuateDePatty.Services
{
    public sealed class JwtSettings
    {
        public const string nombreSeccion = "Jwt";

        public string key { get; set; } = string.Empty;

        public string issuer { get; set; } = string.Empty;

        public string audience { get; set; } = string.Empty;

        public int expirationMinutes { get; set; } = 30;

        public void Validar()
        {
            if (Encoding.UTF8.GetByteCount(key) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key no está configurada o tiene menos de 32 bytes. " +
                    "Defínala mediante variables de entorno (Jwt__Key), user-secrets o un gestor de secretos.");
            }

            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("Jwt:Issuer y Jwt:Audience son obligatorios.");
            }

            if (expirationMinutes < 1 || expirationMinutes > 1440)
            {
                throw new InvalidOperationException("Jwt:ExpirationMinutes debe estar entre 1 y 1440.");
            }
        }
    }
}
