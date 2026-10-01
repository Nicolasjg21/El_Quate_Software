using System.Security.Cryptography;
using System.Text;

namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Claims propios del token y sello derivado del hash de la contraseña:
    /// al cambiar la contraseña el sello cambia y los tokens anteriores dejan de ser válidos.
    /// </summary>
    public static class SelloSeguridad
    {
        public const string claimSello = "sec";

        public const string claimSesion = "sesion";

        public static string Calcular(string passwordHash)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));

            return Convert.ToHexString(bytes, 0, 16);
        }
    }
}
