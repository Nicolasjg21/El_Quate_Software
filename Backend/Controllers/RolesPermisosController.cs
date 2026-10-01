using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.RolesPermisos;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Services;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesPermisosController : ControllerBase
    {
        private readonly IRolesPermisosRepository rolesPermisosRepository;
        private readonly ILogger<RolesPermisosController> logger;

        public RolesPermisosController(
            IRolesPermisosRepository repository,
            ILogger<RolesPermisosController> logger)
        {
            rolesPermisosRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetRolesPermisos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRolesPermisos()
        {
            try
            {
                var rolesPermisos = await rolesPermisosRepository.GetRolesPermisos();

                if (rolesPermisos == null || !rolesPermisos.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron relaciones entre roles y permisos."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Relaciones entre roles y permisos obtenidas correctamente.",
                    data = rolesPermisos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener las relaciones entre roles y permisos."
                });
            }
        }

        [HttpGet("GetRolesPermisosById/{idRol}/{idPermiso}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRolesPermisosById(int idRol, int idPermiso)
        {
            try
            {
                if (idRol <= 0 || idPermiso <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los IDs de rol y permiso deben ser mayores que cero."
                    });
                }

                var rolesPermisos =
                    await rolesPermisosRepository.GetRolesPermisosById(idRol, idPermiso);

                if (rolesPermisos == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró la relación entre el rol y el permiso."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Relación obtenida correctamente.",
                    data = rolesPermisos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener la relación."
                });
            }
        }

        [HttpPost("PostRolesPermisos")]
        [RequierePermiso(PermisosSistema.seguridadGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostRolesPermisos(
            [FromBody] RolPermisoCrearDTO dto)
        {
            try
            {
                var rolesPermisos = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                if (rolesPermisos.idRol <= 0 || rolesPermisos.idPermiso <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID del rol y el ID del permiso deben ser mayores que cero."
                    });
                }

                var existente = await rolesPermisosRepository.GetRolesPermisosById(
                    rolesPermisos.idRol,
                    rolesPermisos.idPermiso);

                if (existente != null)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "La relación entre el rol y el permiso ya existe."
                    });
                }

                var response =
                    await rolesPermisosRepository.PostRolesPermisos(rolesPermisos);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar la relación."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Relación entre rol y permiso registrada correctamente.",
                    data = rolesPermisos.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "La operación no pudo completarse porque viola restricciones de integridad (registro relacionado inexistente o con registros asociados)."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al registrar la relación."
                });
            }
        }

        [HttpPut("PutRolesPermisos")]
        [RequierePermiso(PermisosSistema.seguridadGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutRolesPermisos(
            [FromBody] RolPermisoCrearDTO rolesPermisos)
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

                if (rolesPermisos.idRol <= 0 || rolesPermisos.idPermiso <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID del rol y el ID del permiso deben ser mayores que cero."
                    });
                }

                var existente = await rolesPermisosRepository.GetRolesPermisosById(
                    rolesPermisos.idRol,
                    rolesPermisos.idPermiso);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró la relación que desea actualizar."
                    });
                }

                var response =
                    await rolesPermisosRepository.PutRolesPermisos(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la relación."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Relación actualizada correctamente.",
                    data = existente.ToRespuestaDTO()
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "La operación no pudo completarse porque viola restricciones de integridad (registro relacionado inexistente o con registros asociados)."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al actualizar la relación."
                });
            }
        }

        [HttpDelete("DeleteRolesPermisos/{idRol}/{idPermiso}")]
        [RequierePermiso(PermisosSistema.seguridadGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRolesPermisos(
            int idRol,
            int idPermiso)
        {
            try
            {
                if (idRol <= 0 || idPermiso <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los IDs de rol y permiso deben ser mayores que cero."
                    });
                }

                var rolesPermisos =
                    await rolesPermisosRepository.GetRolesPermisosById(idRol, idPermiso);

                if (rolesPermisos == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró la relación."
                    });
                }

                var response =
                    await rolesPermisosRepository.DeleteRolesPermisos(rolesPermisos);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar la relación."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Relación eliminada correctamente."
                });
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    message = "La operación no pudo completarse porque viola restricciones de integridad (registro relacionado inexistente o con registros asociados)."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al eliminar la relación."
                });
            }
        }
    }
}