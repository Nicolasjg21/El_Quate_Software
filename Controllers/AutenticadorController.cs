using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateSoftware.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AutenticadorController(
            ElQuateDePattyContext context,
            IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            if (login == null ||
                string.IsNullOrEmpty(login.Email) ||
                string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Los datos de inicio de sesión son obligatorios.");
            }

            
            var usuario = await context.Usuarios
                .FirstOrDefaultAsync(u => u.email == login.Email);

            if (usuario == null)
            {
                return Unauthorized("Usuario o contraseña incorrectos.");
            }

            
            if (usuario.passwordHash != login.Password)
            {
                return Unauthorized("Usuario o contraseña incorrectos.");
            }

           
            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
            );

            var signinCredentials = new SigningCredentials(
                secretKey,
                SecurityAlgorithms.HmacSha256
            );

            var tokenOptions = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.email),
                    new Claim(ClaimTypes.Role, usuario.idRol.ToString())
                },
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: signinCredentials
            );
            // nuevo cambio
            var tokenString =
                new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return Ok(new
            {
                Token = tokenString
            });
        }
    }
}