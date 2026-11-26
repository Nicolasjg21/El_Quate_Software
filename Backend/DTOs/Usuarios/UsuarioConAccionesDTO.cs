namespace ElQuateDePatty.DTOs.Usuarios
{
    /// <summary>
    /// Usuario del listado con las acciones que el usuario de la sesión puede hacer sobre él.
    /// Las calcula el servidor con ReglasUsuarios: la pantalla no repite las reglas.
    /// </summary>
    public class UsuarioConAccionesDTO : UsuarioRespuestaDTO
    {
        public AccionesUsuarioDTO acciones { get; set; } = new();
    }

    public class AccionesUsuarioDTO
    {
        public bool editar { get; set; }

        public bool eliminar { get; set; }

        public bool restablecerPassword { get; set; }
    }
}
