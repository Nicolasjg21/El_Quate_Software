using Microsoft.AspNetCore.Authorization;

namespace ElQuateDePatty.Services
{
    public sealed class PermisoRequirement : IAuthorizationRequirement
    {
        public PermisoRequirement(string permiso)
        {
            this.permiso = permiso;
        }

        public string permiso { get; }
    }
}
