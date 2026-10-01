using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ElQuateDePatty.Services
{
    public sealed class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
    {
        private readonly AutorizacionSettings autorizacionSettings;
        private readonly IPermisosRolService permisosRolService;

        public PermisoAuthorizationHandler(
            AutorizacionSettings autorizacionSettings,
            IPermisosRolService permisosRolService)
        {
            this.autorizacionSettings = autorizacionSettings;
            this.permisosRolService = permisosRolService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermisoRequirement requirement)
        {
            // Cuenta de emergencia definida por configuración (opcional).
            if (autorizacionSettings.EsAdministrador(context.User))
            {
                context.Succeed(requirement);
                return;
            }

            var rol = context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(rol, out var idRol))
            {
                return;
            }

            if (await permisosRolService.TienePermiso(idRol, requirement.permiso))
            {
                context.Succeed(requirement);
            }
        }
    }
}
