using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateSoftware.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticadorController : ControllerBase
    {
        private readonly ElQuateDePattyContext context;
        private readonly IConfiguration configuration;
        private readonly PasswordHasher<Usuarios> passwordHasher;

        public AutenticadorController(
            ElQuateDePattyContext context,
            IConfiguration configuration,
            PasswordHasher<Usuarios> passwordHasher)
        {
            this.context = context;
            this.configuration = configuration;
            this.passwordHasher = passwordHasher;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            // Validar que lleguen los datos
            if (login == null ||
                string.IsNullOrWhiteSpace(login.Email) ||
                string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = "El correo y la contraseña son obligatorios."
                });
            }

            // Buscar usuario por correo
            var usuario = await context.Usuarios
                .FirstOrDefaultAsync(u => u.email == login.Email);

            // Usuario no encontrado
            if (usuario == null)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = "Usuario o contraseña incorrectos."
                });
            }

            // Verificar que el usuario esté activo
            if (!usuario.estado)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = "El usuario se encuentra inactivo."
                });
            }

            // Verificar contraseña utilizando el hash almacenado
            var resultado = passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.passwordHash,
                login.Password
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = "Usuario o contraseña incorrectos."
                });
            }

            // Obtener la clave JWT
            var jwtKey = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "La clave JWT no está configurada."
                });
            }

            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var signinCredentials = new SigningCredentials(
                secretKey,
                SecurityAlgorithms.HmacSha256
            );

            // Crear claims
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Name,
                    usuario.email
                ),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.idUsuario.ToString()
                ),

                new Claim(
                    ClaimTypes.Role,
                    usuario.idRol.ToString()
                )
            };

            // Crear token
            var tokenOptions = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: signinCredentials
            );

            // Convertir token a string
            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(tokenOptions);

            // Devolver token
            return Ok(new
            {
                statusCode = 200,
                message = "Inicio de sesión exitoso.",
                token = tokenString
            });
        }
    }
}