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

        public const string claimInicio = "inicio";

        /// <summary>Presente ("true") mientras el usuario tenga una contraseña temporal sin cambiar.</summary>
        public const string claimCambioPendiente = "cambio";

        public static string Calcular(string passwordHash)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));

            return Convert.ToHexString(bytes, 0, 16);
        }
    }
}