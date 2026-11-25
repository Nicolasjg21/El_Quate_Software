using ElQuateDePatty.Models;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Emite los JWT de sesión. Cada token dura Jwt:ExpirationMinutes y se puede renovar
    /// mientras el usuario trabaja, sin pasar de Jwt:SesionMaximaHoras desde el inicio de sesión.
    /// </summary>
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings jwtSettings;

        public TokenService(JwtSettings jwtSettings)
        {
            this.jwtSettings = jwtSettings;
        }

        public TokenResultado GenerarToken(Usuarios usuario)
        {
            return Crear(usuario, Guid.NewGuid().ToString(), DateTime.UtcNow);
        }

        public TokenResultado? RenovarToken(Usuarios usuario, string idSesion, DateTime inicioSesionUtc)
        {
            var limite = DateTime.SpecifyKind(inicioSesionUtc, DateTimeKind.Utc).AddHours(jwtSettings.sesionMaximaHoras);

            if (DateTime.UtcNow >= limite)
            {
                return null;
            }

            return Crear(usuario, idSesion, inicioSesionUtc);
        }

        private TokenResultado Crear(Usuarios usuario, string idSesion, DateTime inicioSesionUtc)
        {
            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.key)),
                SecurityAlgorithms.HmacSha256);

            var inicioUtc = DateTime.SpecifyKind(inicioSesionUtc, DateTimeKind.Utc);
            var inicioUnix = new DateTimeOffset(inicioUtc).ToUnixTimeSeconds();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.email),
                new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
                new Claim(ClaimTypes.Role, usuario.idRol.ToString()),
                // Identificador único de este token; la sesión se mantiene entre renovaciones.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(SelloSeguridad.claimSesion, idSesion),
                new Claim(SelloSeguridad.claimSello, SelloSeguridad.Calcular(usuario.passwordHash)),
                new Claim(SelloSeguridad.claimInicio, inicioUnix.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
            };

            var ahora = DateTime.UtcNow;
            var expiraEn = ahora.AddMinutes(jwtSettings.expirationMinutes);
            var limite = inicioUtc.AddHours(jwtSettings.sesionMaximaHoras);

            // Un token nunca vive más allá de la duración máxima de la sesión.
            if (expiraEn > limite)
            {
                expiraEn = limite;
            }

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