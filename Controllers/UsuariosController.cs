using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosRepository _repository;
        private readonly PasswordHasher<Usuarios> _passwordHasher;

        public UsuariosController(
            IUsuariosRepository repository,
            PasswordHasher<Usuarios> passwordHasher)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
        }

        [HttpGet("GetUsuarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var usuarios = await _repository.GetUsuarios();

                if (usuarios == null || !usuarios.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron usuarios."
                    });
                }

                foreach (var usuario in usuarios)
                {
                    usuario.passwordHash = null;
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuarios obtenidos correctamente.",
                    data = usuarios
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los usuarios."
                });
            }
        }

        [HttpGet("GetUsuariosById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsuariosById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var usuario = await _repository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                usuario.passwordHash = null;

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario obtenido correctamente.",
                    data = usuario
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el usuario."
                });
            }
        }

        [HttpPost("PostUsuarios")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostUsuarios([FromBody] Usuarios usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                if (string.IsNullOrWhiteSpace(usuario.passwordHash))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La contraseña es obligatoria."
                    });
                }

                usuario.passwordHash = _passwordHasher.HashPassword(usuario, usuario.passwordHash);

                var response = await _repository.PostUsuarios(usuario);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el usuario."
                    });
                }

                usuario.passwordHash = null;

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Usuario registrado correctamente.",
                    data = usuario
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
                    message = "Ocurrió un error interno al registrar el usuario."
                });
            }
        }

        [HttpPut("PutUsuarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutUsuarios([FromBody] Usuarios usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                if (usuario.idUsuario <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await _repository.GetUsuariosById(usuario.idUsuario);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario que desea actualizar."
                    });
                }

                existente.nombres = usuario.nombres;
                existente.apellidos = usuario.apellidos;
                existente.documento = usuario.documento;
                existente.idTipoDocumento = usuario.idTipoDocumento;
                existente.telefono = usuario.telefono;
                existente.estado = usuario.estado;
                existente.idRol = usuario.idRol;
                existente.email = usuario.email;

                if (!string.IsNullOrWhiteSpace(usuario.passwordHash))
                {
                    existente.passwordHash =
                        _passwordHasher.HashPassword(
                            existente,
                            usuario.passwordHash);
                }

                var response = await _repository.PutUsuarios(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el usuario."
                    });
                }

                existente.passwordHash = null;

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario actualizado correctamente.",
                    data = existente
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }

        [HttpDelete("DeleteUsuarios/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUsuarios(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var usuario = await _repository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                var response = await _repository.DeleteUsuarios(usuario);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el usuario."
                    });
                }

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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al eliminar el usuario."
                });
            }
        }
    }
}