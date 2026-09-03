using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosRepository _usuariosRepository;
        private readonly PasswordHasher<Usuarios> _passwordHasher;

        public UsuariosController(IUsuariosRepository usuariosRepository)
        {
            _usuariosRepository = usuariosRepository;
            _passwordHasher = new PasswordHasher<Usuarios>();
        }

        [HttpGet("GetUsuarios")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var response = await _usuariosRepository.GetUsuarios();

                if (response == null || response.Count == 0)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró información de usuarios."
                    });

                foreach (var usuario in response)
                    usuario.passwordHash = string.Empty;

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de usuarios realizada correctamente.",
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { statusCode = 404, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpGet("GetUsuarioById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUsuarioById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El identificador debe ser mayor que cero."
                    });

                var response = await _usuariosRepository.GetUsuariosById(id);

                if (response == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El usuario solicitado no existe."
                    });

                response.passwordHash = string.Empty;

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario encontrado correctamente.",
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { statusCode = 404, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPost("PostUsuario")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostUsuario([FromBody] Usuarios usuarios)
        {
            try
            {
                if (!ModelState.IsValid || usuarios == null)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del usuario no son válidos."
                    });

                if (string.IsNullOrWhiteSpace(usuarios.passwordHash))
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La contraseña es obligatoria."
                    });

                usuarios.passwordHash =
                    _passwordHasher.HashPassword(usuarios, usuarios.passwordHash);

                var response = await _usuariosRepository.PostUsuarios(usuarios);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el usuario."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario registrado correctamente."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPut("PutUsuario")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutUsuario([FromBody] Usuarios usuarios)
        {
            try
            {
                if (!ModelState.IsValid || usuarios == null || usuarios.idUsuario <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del usuario no son válidos."
                    });

                var existente = await _usuariosRepository.GetUsuariosById(usuarios.idUsuario);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El usuario que se desea actualizar no existe."
                    });

                if (!string.IsNullOrWhiteSpace(usuarios.passwordHash))
                {
                    usuarios.passwordHash =
                        _passwordHasher.HashPassword(usuarios, usuarios.passwordHash);
                }
                else
                {
                    usuarios.passwordHash = existente.passwordHash;
                }

                var response = await _usuariosRepository.PutUsuarios(usuarios);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el usuario."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario actualizado correctamente."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpDelete("DeleteUsuario/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El identificador debe ser mayor que cero."
                    });

                var usuario = await _usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El usuario que se desea eliminar no existe."
                    });

                var response = await _usuariosRepository.DeleteUsuarios(usuario);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el usuario."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario eliminado correctamente."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }
    }
}