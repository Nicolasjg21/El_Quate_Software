using ElQuateDePatty.Models;

namespace ElQuateDePatty.Services
{
    public interface ITokenService
    {
        TokenResultado GenerarToken(Usuarios usuario);

        /// <summary>Token nuevo para la misma sesión. Null si la sesión ya superó Jwt:SesionMaximaHoras.</summary>
        TokenResultado? RenovarToken(Usuarios usuario, string idSesion, DateTime inicioSesionUtc);
    }
}