namespace ElQuateDePatty.Services
{
    public static class PoliticasAutorizacion
    {
        public const string prefijoPermiso = "permiso:";

        public static string Permiso(string nombrePermiso)
        {
            return prefijoPermiso + nombrePermiso;
        }
    }
}
