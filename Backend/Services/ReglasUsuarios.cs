namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Quién puede administrar a quién. Reglas puras (sin base de datos) que el servidor aplica
    /// antes de tocar un usuario; la pantalla solo las refleja ocultando botones.
    /// Cada método devuelve null si la acción está permitida o el motivo del rechazo (403).
    /// "Administrador" es cualquier usuario cuyo rol tiene usuarios.gestionar.
    /// </summary>
    public sealed class ReglasUsuarios
    {
        private const string soporteIntocable = "La cuenta de Soporte técnico no se puede modificar.";

        private readonly UsuariosProtegidosSettings protegidos;

        public ReglasUsuarios(UsuariosProtegidosSettings protegidos)
        {
            this.protegidos = protegidos;
        }

        public bool EsSoporte(int idUsuario) => idUsuario == protegidos.idSoporte;

        public bool EsAdministradora(int idUsuario) => idUsuario == protegidos.idAdministradora;

        /// <summary>Soporte técnico o la administradora principal.</summary>
        public bool EsSuperior(int idUsuario) => EsSoporte(idUsuario) || EsAdministradora(idUsuario);

        public string? Crear(int idActor, bool rolNuevoEsAdministrador)
        {
            if (rolNuevoEsAdministrador && !EsSuperior(idActor))
            {
                return "Solo Soporte técnico o la administradora principal pueden crear usuarios administradores.";
            }

            return null;
        }

        public string? Editar(int idActor, int idObjetivo, bool objetivoEsAdministrador, bool rolNuevoEsAdministrador)
        {
            if (EsSoporte(idObjetivo))
            {
                return soporteIntocable;
            }

            // Incluye no cambiarse el propio rol ni el propio estado (el sistema podría quedar sin administrador).
            if (idActor == idObjetivo)
            {
                return "No puede modificar su propio usuario desde aquí. Su teléfono y su contraseña se cambian en Configuración.";
            }

            if (EsAdministradora(idObjetivo) && !EsSoporte(idActor))
            {
                return "Solo Soporte técnico puede modificar a la administradora principal.";
            }

            if (objetivoEsAdministrador && !EsSuperior(idActor))
            {
                return "Solo Soporte técnico o la administradora principal pueden modificar a otro administrador.";
            }

            if (rolNuevoEsAdministrador && !EsSuperior(idActor))
            {
                return "Solo Soporte técnico o la administradora principal pueden asignar el rol de administrador.";
            }

            return null;
        }

        public string? Eliminar(int idActor, int idObjetivo)
        {
            if (EsSoporte(idObjetivo))
            {
                return "La cuenta de Soporte técnico no se puede eliminar.";
            }

            if (idActor == idObjetivo)
            {
                return "No puede eliminar su propio usuario.";
            }

            if (!EsSuperior(idActor))
            {
                return "Solo Soporte técnico o la administradora principal pueden eliminar usuarios.";
            }

            if (EsAdministradora(idObjetivo) && !EsSoporte(idActor))
            {
                return "Solo Soporte técnico puede eliminar a la administradora principal.";
            }

            return null;
        }

        public string? Restablecer(int idActor, int idObjetivo)
        {
            if (EsSoporte(idObjetivo))
            {
                return "La contraseña de Soporte técnico no se puede restablecer.";
            }

            if (idActor == idObjetivo)
            {
                return "Para cambiar su propia contraseña use Configuración.";
            }

            if (!EsSuperior(idActor))
            {
                return "Solo Soporte técnico o la administradora principal pueden restablecer contraseñas.";
            }

            if (EsAdministradora(idObjetivo) && !EsSoporte(idActor))
            {
                return "Solo Soporte técnico puede restablecer la contraseña de la administradora principal.";
            }

            return null;
        }

        /// <summary>Teléfono propio (Mi perfil). Los demás datos de identidad solo los cambia la administración.</summary>
        public string? EditarPerfilPropio(int idActor)
        {
            return EsSoporte(idActor) ? soporteIntocable : null;
        }
    }
}
