using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ElQuateDePatty.Services
{
    public sealed class PermisoPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermisoPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PoliticasAutorizacion.prefijoPermiso, StringComparison.Ordinal))
            {
                var permiso = policyName.Substring(PoliticasAutorizacion.prefijoPermiso.Length);

                var politica = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermisoRequirement(permiso))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(politica);
            }

            return base.GetPolicyAsync(policyName);
        }
    }
}
