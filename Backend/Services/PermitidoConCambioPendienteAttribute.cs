namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Marca las acciones que un usuario con contraseña temporal puede usar antes de cambiarla
    /// (cambiar la contraseña, renovar o cerrar la sesión y registrar su auditoría).
    /// Cualquier otra acción responde 403 con el código CAMBIO_PASSWORD_OBLIGATORIO.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PermitidoConCambioPendienteAttribute : Attribute
    {
        public const string codigoError = "CAMBIO_PASSWORD_OBLIGATORIO";
    }
}
