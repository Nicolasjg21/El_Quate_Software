using System.Security.Cryptography;

namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Contraseñas temporales para el restablecimiento por la administración.
    /// Aleatorias con un generador criptográfico; sin caracteres que se confundan al dictarlas
    /// (0/O, 1/l/I). Se muestran una sola vez y nunca se guardan en claro.
    /// </summary>
    public static class GeneradorPasswordTemporal
    {
        public const int longitud = 12;

        public static readonly TimeSpan vigencia = TimeSpan.FromHours(24);

        private const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static string Generar()
        {
            return RandomNumberGenerator.GetString(alfabeto, longitud);
        }
    }
}
