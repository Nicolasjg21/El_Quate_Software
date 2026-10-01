using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosRepository usuariosRepository;
        private readonly IPasswordHasher<Usuarios> passwordHasher;
        private readonly IAuthorizationService authorizationService;
        private readonly ITokenService tokenService;
        private readonly ILogger<UsuariosController> logger;

        public UsuariosController(
            IUsuariosRepository repository,
            IPasswordHasher<Usuarios> passwordHasher,
            IAuthorizationService authorizationService,
            ITokenService tokenService,
            ILogger<UsuariosController> logger)
        {
            usuariosRepository = repository;
            this.passwordHasher = passwordHasher;
            this.authorizationService = authorizationService;
            this.tokenService = tokenService;
            this.logger = logger;
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
                var usuarios = await usuariosRepository.GetUsuarios();

                if (usuarios == null || !usuarios.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron usuarios."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuarios obtenidos correctamente.",
                    data = usuarios.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al obtener los usuarios.");
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

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario obtenido correctamente.",
                    data = usuario.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al obtener el usuario.");
            }
        }

        // POST: api/Usuarios/PostUsuarios
        [HttpPost("PostUsuarios")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
                        errors = new ValidationProblemDetails(ModelState).Errors
                    });
                }

                if (await usuariosRepository.ExisteEmail(dto.email))
                {
                    return Conflict(new
                    {
                        statusCode = 409,
                        message = "Ya existe un usuario registrado con ese correo electrónico."
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

                usuario.passwordHash = passwordHasher.HashPassword(usuario, dto.password);

                var response = await usuariosRepository.PostUsuarios(usuario);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el usuario."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Usuario registrado correctamente.",
                    data = usuario.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "No fue posible guardar el usuario: el correo ya existe o el rol/tipo de documento indicado no es válido."
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al registrar el usuario.");
            }
        }

        // PUT: api/Usuarios/PutUsuarios/1
        [HttpPut("PutUsuarios/{id}")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
                        errors = new ValidationProblemDetails(ModelState).Errors
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

                var existente = await usuariosRepository.GetUsuariosById(id);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario que desea actualizar."
                    });
                }

                if (await usuariosRepository.ExisteEmail(dto.email, id))
                {
                    return Conflict(new
                    {
                        statusCode = 409,
                        message = "Ya existe un usuario registrado con ese correo electrónico."
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

                var response = await usuariosRepository.PutUsuarios(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el usuario."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Usuario actualizado correctamente.",
                    data = existente.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "No fue posible guardar el usuario: el correo ya existe o el rol/tipo de documento indicado no es válido."
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al actualizar el usuario.");
            }
        }

        // PUT: api/Usuarios/CambiarPassword/1
        [HttpPut("CambiarPassword/{id}")]
        [EnableRateLimiting("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CambiarPassword(
            int id,
            [FromBody] CambiarPasswordDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = new ValidationProblemDetails(ModelState).Errors
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

                var esPropietario = User.ObtenerIdUsuario() == id;
                var esAdministrador = (await authorizationService
                    .AuthorizeAsync(User, PoliticasAutorizacion.Permiso(PermisosSistema.usuariosGestionar))).Succeeded;

                if (!esPropietario && !esAdministrador)
                {
                    return StatusCode(403, new
                    {
                        statusCode = 403,
                        message = "No tiene permisos para cambiar la contraseña de otro usuario."
                    });
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                var resultado = passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.passwordHash,
                    dto.passwordActual);

                if (resultado == PasswordVerificationResult.Failed)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La contraseña actual es incorrecta."
                    });
                }

                if (dto.nuevaPassword != dto.confirmarPassword)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La nueva contraseña y la confirmación no coinciden."
                    });
                }

                usuario.passwordHash = passwordHasher.HashPassword(usuario, dto.nuevaPassword);

                var response = await usuariosRepository.PutUsuarios(usuario);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la contraseña."
                    });
                }

                // Al cambiar la contraseña el sello de seguridad cambia y el token actual
                // deja de ser válido; se entrega uno nuevo cuando quien cambia es el propio usuario.
                if (esPropietario)
                {
                    var nuevoToken = tokenService.GenerarToken(usuario);

                    return Ok(new
                    {
                        statusCode = 200,
                        message = "Contraseña actualizada correctamente.",
                        token = nuevoToken.token,
                        expiraEn = nuevoToken.expiraEn
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Contraseña actualizada correctamente."
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al actualizar la contraseña.");
            }
        }

        // DELETE: api/Usuarios/DeleteUsuarios/1
        [HttpDelete("DeleteUsuarios/{id}")]
        [RequierePermiso(PermisosSistema.usuariosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

                if (User.ObtenerIdUsuario() == id)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No puede eliminar su propio usuario."
                    });
                }

                var usuario = await usuariosRepository.GetUsuariosById(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el usuario."
                    });
                }

                var response = await usuariosRepository.DeleteUsuarios(usuario);

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
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "No se pudo eliminar el usuario {IdUsuario} por registros asociados", id);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "No es posible eliminar el usuario porque tiene registros asociados. Puede desactivarlo."
                });
            }
            catch (Exception ex)
            {
                return ErrorInterno(ex, "Ocurrió un error interno al eliminar el usuario.");
            }
        }

        private IActionResult ErrorInterno(Exception ex, string mensaje)
        {
            logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

            return StatusCode(500, new
            {
                statusCode = 500,
                message = mensaje
            });
        }
    }
}
