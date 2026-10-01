namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Nombres de permisos que deben existir en la tabla Permisos
    /// y asignarse a los roles mediante RolesPermisos (ver Database/permisos-iniciales.sql).
    /// </summary>
    public static class PermisosSistema
    {
        public const string usuariosGestionar = "usuarios.gestionar";

        public const string seguridadGestionar = "seguridad.gestionar";

        public const string kardexModificar = "kardex.modificar";

        public const string auditoriasConsultar = "auditorias.consultar";

        public const string auditoriasModificar = "auditorias.modificar";
    }
}
