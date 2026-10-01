using System.Security.Claims;

namespace ElQuateDePatty.Services
{
    public sealed class AutorizacionSettings
    {
        public IReadOnlySet<string> rolesAdministrador { get; init; } = new HashSet<string>();

        public bool EsAdministrador(ClaimsPrincipal principal)
        {
            var rol = principal.FindFirst(ClaimTypes.Role)?.Value;

            return rol != null && rolesAdministrador.Contains(rol);
        }
    }
}
