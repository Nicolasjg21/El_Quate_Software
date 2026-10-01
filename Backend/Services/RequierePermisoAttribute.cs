using Microsoft.AspNetCore.Authorization;

namespace ElQuateDePatty.Services
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequierePermisoAttribute : AuthorizeAttribute
    {
        public RequierePermisoAttribute(string permiso)
        {
            Policy = PoliticasAutorizacion.Permiso(permiso);
        }
    }
}
