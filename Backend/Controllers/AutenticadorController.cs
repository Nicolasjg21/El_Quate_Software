using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticadorController : ControllerBase
    {
        private const string mensajeCredencialesInvalidas = "Usuario o contraseña incorrectos.";

        private readonly IUsuariosRepository usuariosRepository;
        private readonly IPasswordHasher<Usuarios> passwordHasher;
        private static string? hashFicticio;

        private readonly ITokenService tokenService;
        private readonly IRastreadorIntentosLogin rastreadorIntentos;
        private readonly IRevocacionTokenService revocacionTokenService;
        private readonly ILogger<AutenticadorController> logger;

        public AutenticadorController(
            IUsuariosRepository usuariosRepository,
            IPasswordHasher<Usuarios> passwordHasher,
            ITokenService tokenService,
            IRastreadorIntentosLogin rastreadorIntentos,
            IRevocacionTokenService revocacionTokenService,
            ILogger<AutenticadorController> logger)
        {
            this.usuariosRepository = usuariosRepository;
            this.passwordHasher = passwordHasher;
            this.tokenService = tokenService;
            this.rastreadorIntentos = rastreadorIntentos;
            this.revocacionTokenService = revocacionTokenService;
            this.logger = logger;
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            try
            {
                if (login == null ||
                    string.IsNullOrWhiteSpace(login.email) ||
                    string.IsNullOrWhiteSpace(login.password))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El correo y la contraseña son obligatorios."
                    });
                }

                var email = login.email.Trim();

                if (rastreadorIntentos.EstaBloqueado(email))
                {
                    return StatusCode(429, new
                    {
                        statusCode = 429,
                        message = "Demasiados intentos fallidos. Intente nuevamente en unos minutos."
                    });
                }

                var usuario = await usuariosRepository.GetUsuariosByEmail(email);

                if (usuario == null)
                {
                    // Se verifica contra un hash ficticio (calculado una sola vez) para igualar
                    // el tiempo de respuesta y evitar la enumeración de usuarios por temporización.
                    hashFicticio ??= passwordHasher.HashPassword(new Usuarios(), Guid.NewGuid().ToString());

                    passwordHasher.VerifyHashedPassword(new Usuarios(), hashFicticio, login.password);

                    rastreadorIntentos.RegistrarFallo(email);

                    return Credenciales();
                }

                var resultado = passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.passwordHash,
                    login.password);

                if (resultado == PasswordVerificationResult.Failed)
                {
                    rastreadorIntentos.RegistrarFallo(email);

                    return Credenciales();
                }

                rastreadorIntentos.Limpiar(email);

                // El estado se revela únicamente cuando la contraseña es correcta.
                if (!usuario.estado)
                {
                    return Unauthorized(new
                    {
                        statusCode = 401,
                        message = "El usuario se encuentra inactivo."
                    });
                }

                if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    usuario.passwordHash = passwordHasher.HashPassword(usuario, login.password);
                    await usuariosRepository.PutUsuarios(usuario);
                }

                var token = tokenService.GenerarToken(usuario);

                return Ok(new
                {
                    statusCode = 200,
                    message = "Inicio de sesión exitoso.",
                    token = token.token,
                    expiraEn = token.expiraEn
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al iniciar sesión."
                });
            }
        }

        [HttpPost("Logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            var idSesion = User.FindFirst(SelloSeguridad.claimSesion)?.Value;
            var exp = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (!string.IsNullOrEmpty(idSesion) &&
                long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var segundos))
            {
                revocacionTokenService.Revocar(idSesion, DateTimeOffset.FromUnixTimeSeconds(segundos));
            }

            return Ok(new
            {
                statusCode = 200,
                message = "Sesión cerrada correctamente."
            });
        }

        private IActionResult Credenciales()
        {
            return Unauthorized(new
            {
                statusCode = 401,
                message = mensajeCredencialesInvalidas
            });
        }

        /// <summary>
        /// Renueva el token de una sesión activa: entrega uno nuevo con la misma sesión y el mismo
        /// inicio, válido otros Jwt:ExpirationMinutes sin pasar de Jwt:SesionMaximaHoras.
        /// Antes de llegar aquí, el middleware ya validó el token actual: usuario activo, rol,
        /// contraseña y sesión no revocada.
        /// </summary>
        [HttpPost("Renovar")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Renovar()
        {
            try
            {
                var idUsuario = User.ObtenerIdUsuario();
                var idSesion = User.FindFirst(SelloSeguridad.claimSesion)?.Value;
                var inicio = User.FindFirst(SelloSeguridad.claimInicio)?.Value;

                // Los tokens emitidos antes de este cambio no traen "inicio": se pide un login nuevo.
                if (idUsuario == null || string.IsNullOrEmpty(idSesion) ||
                    !long.TryParse(inicio, NumberStyles.Integer, CultureInfo.InvariantCulture, out var inicioUnix))
                {
                    return SesionNoRenovable("La sesión no se puede renovar. Inicie sesión nuevamente.");
                }

                var usuario = await usuariosRepository.GetUsuariosById(idUsuario.Value);

                if (usuario == null || !usuario.estado)
                {
                    return SesionNoRenovable("El usuario se encuentra inactivo.");
                }

                var token = tokenService.RenovarToken(
                    usuario, idSesion, DateTimeOffset.FromUnixTimeSeconds(inicioUnix).UtcDateTime);

                if (token == null)
                {
                    return SesionNoRenovable("La sesión alcanzó su duración máxima. Inicie sesión nuevamente.");
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Sesión renovada.",
                    token = token.token,
                    expiraEn = token.expiraEn
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al renovar la sesión."
                });
            }
        }

        private IActionResult SesionNoRenovable(string mensaje)
        {
            return Unauthorized(new { statusCode = 401, message = mensaje });
        }


    }
}
