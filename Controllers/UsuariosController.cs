using ElQuateDePatty.DTOs;
using ElQuateDePatty.Models;
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
        private readonly IUsuariosRepository _usuariosRepository;
        private readonly PasswordHasher<Usuarios> _passwordHasher;

        public UsuariosController(
            IUsuariosRepository repository,
            PasswordHasher<Usuarios> passwordHasher)
        {
            _usuariosRepository = repository;
            _passwordHasher = passwordHasher;
        }

        // GET: api/Usuarios/GetUsuarios
        [HttpGet("GetUsuarios")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var usuarios = await _usuariosRepository.GetUsuarios();

                if (usuarios == null || !usuarios.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron usuarios."
                    });
                }

                var respuesta = usuarios.Select(usuario =>
                    new UsuarioRespuestaDTO
                    {
                        idUsuario = usuario.idUsuario,
                        nombres = usuario.nombres,
                        apellidos = usuario.apellidos,
                        documento = usuario.documento,
                        idTipoDocumento = usuario.idTipoDocumento,
                        telefono = usuario.telefono,
                        estado = usuario.estado,
                        idRol = usuario.idRol,
                        email = usuario.email
                    }).ToList();

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuarios obtenidos correctamente.",
                    data = respuesta
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

        // GET: api/Usuarios/GetUsuariosById/1
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

                var usuario = await _usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                var respuesta = new UsuarioRespuestaDTO
                {
                    idUsuario = usuario.idUsuario,
                    nombres = usuario.nombres,
                    apellidos = usuario.apellidos,
                    documento = usuario.documento,
                    idTipoDocumento = usuario.idTipoDocumento,
                    telefono = usuario.telefono,
                    estado = usuario.estado,
                    idRol = usuario.idRol,
                    email = usuario.email
                };

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario obtenido correctamente.",
                    data = respuesta
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

        // POST: api/Usuarios/PostUsuarios
        [HttpPost("PostUsuarios")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostUsuarios(
            [FromBody] UsuarioCrearDTO dto)
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

                if (string.IsNullOrWhiteSpace(dto.password))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La contraseña es obligatoria."
                    });
                }

                var usuario = new Usuarios
                {
                    nombres = dto.nombres,
                    apellidos = dto.apellidos,
                    documento = dto.documento,
                    idTipoDocumento = dto.idTipoDocumento,
                    telefono = dto.telefono,
                    estado = dto.estado,
                    idRol = dto.idRol,
                    email = dto.email
                };

                usuario.passwordHash =
                    _passwordHasher.HashPassword(
                        usuario,
                        dto.password);

                var response =
                    await _usuariosRepository.PostUsuarios(usuario);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el usuario."
                    });
                }

                var respuesta = new UsuarioRespuestaDTO
                {
                    idUsuario = usuario.idUsuario,
                    nombres = usuario.nombres,
                    apellidos = usuario.apellidos,
                    documento = usuario.documento,
                    idTipoDocumento = usuario.idTipoDocumento,
                    telefono = usuario.telefono,
                    estado = usuario.estado,
                    idRol = usuario.idRol,
                    email = usuario.email
                };

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Usuario registrado correctamente.",
                    data = respuesta
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

        // PUT: api/Usuarios/PutUsuarios/1
        [HttpPut("PutUsuarios/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutUsuarios(
            int id,
            [FromBody] UsuarioActualizarDTO dto)
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

                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente =
                    await _usuariosRepository.GetUsuariosById(id);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario que desea actualizar."
                    });
                }

                existente.nombres = dto.nombres;
                existente.apellidos = dto.apellidos;
                existente.documento = dto.documento;
                existente.idTipoDocumento = dto.idTipoDocumento;
                existente.telefono = dto.telefono;
                existente.estado = dto.estado;
                existente.idRol = dto.idRol;
                existente.email = dto.email;

                // Si se envía una nueva contraseña, se actualiza.
                // Si viene vacía, se conserva la contraseña actual.
                if (!string.IsNullOrWhiteSpace(dto.password))
                {
                    existente.passwordHash =
                        _passwordHasher.HashPassword(
                            existente,
                            dto.password);
                }

                var response =
                    await _usuariosRepository.PutUsuarios(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el usuario."
                    });
                }

                var respuesta = new UsuarioRespuestaDTO
                {
                    idUsuario = existente.idUsuario,
                    nombres = existente.nombres,
                    apellidos = existente.apellidos,
                    documento = existente.documento,
                    idTipoDocumento = existente.idTipoDocumento,
                    telefono = existente.telefono,
                    estado = existente.estado,
                    idRol = existente.idRol,
                    email = existente.email
                };

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario actualizado correctamente.",
                    data = respuesta
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al actualizar el usuario."
                });
            }
        }

        // DELETE: api/Usuarios/DeleteUsuarios/1
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

                var usuario =
                    await _usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                var response =
                    await _usuariosRepository.DeleteUsuarios(usuario);

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