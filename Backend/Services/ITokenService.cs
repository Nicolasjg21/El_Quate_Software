using ElQuateDePatty.Models;

namespace ElQuateDePatty.Services
{
    public interface ITokenService
    {
        TokenResultado GenerarToken(Usuarios usuario);
    }
}
