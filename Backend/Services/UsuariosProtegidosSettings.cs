namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Cuentas con protección especial (sección "UsuariosProtegidos" de la configuración).
    /// Soporte técnico no lo modifica nadie; la administradora principal solo la modifica Soporte técnico.
    /// Ambos son los únicos que eliminan usuarios, restablecen contraseñas y asignan el rol de administrador.
    /// </summary>
    public sealed class UsuariosProtegidosSettings
    {
        public const string nombreSeccion = "UsuariosProtegidos";

        public int idSoporte { get; set; }

        public int idAdministradora { get; set; }

        public void Validar()
        {
            if (idSoporte <= 0 || idAdministradora <= 0)
            {
                throw new InvalidOperationException(
                    "UsuariosProtegidos:IdSoporte y UsuariosProtegidos:IdAdministradora son obligatorios y deben ser mayores que cero.");
            }

            if (idSoporte == idAdministradora)
            {
                throw new InvalidOperationException(
                    "UsuariosProtegidos:IdSoporte y UsuariosProtegidos:IdAdministradora deben ser usuarios distintos.");
            }
        }
    }
}
