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

        /// <summary>
        /// Administración de inventario y compras. En Kardex se exige para los ajustes
        /// manuales de stock (tipo AJUSTE); las entradas y salidas no lo requieren.
        /// </summary>
        public const string inventarioGestionar = "inventario.gestionar";
    }
}