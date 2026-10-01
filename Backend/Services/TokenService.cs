using ElQuateDePatty.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElQuateDePatty.Services
{
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings jwtSettings;

        public TokenService(JwtSettings jwtSettings)
        {
            this.jwtSettings = jwtSettings;
        }

        public TokenResultado GenerarToken(Usuarios usuario)
        {
            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.key)),
                SecurityAlgorithms.HmacSha256);

            var idSesion = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.email),
                new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
                new Claim(ClaimTypes.Role, usuario.idRol.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, idSesion),
                new Claim(SelloSeguridad.claimSesion, idSesion),
                new Claim(SelloSeguridad.claimSello, SelloSeguridad.Calcular(usuario.passwordHash))
            };

            var ahora = DateTime.UtcNow;
            var expiraEn = ahora.AddMinutes(jwtSettings.expirationMinutes);

            var jwt = new JwtSecurityToken(
                issuer: jwtSettings.issuer,
                audience: jwtSettings.audience,
                claims: claims,
                notBefore: ahora,
                expires: expiraEn,
                signingCredentials: credenciales);

            return new TokenResultado
            {
                token = new JwtSecurityTokenHandler().WriteToken(jwt),
                expiraEn = expiraEn
            };
        }
    }
}
