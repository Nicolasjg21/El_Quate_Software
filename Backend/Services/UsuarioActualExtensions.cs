using System.Security.Claims;

namespace ElQuateDePatty.Services
{
    public static class UsuarioActualExtensions
    {
        public static int? ObtenerIdUsuario(this ClaimsPrincipal principal)
        {
            var valor = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(valor, out var id) ? id : null;
        }
    }
}
