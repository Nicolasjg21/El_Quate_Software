using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Auditorias;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Services;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditoriasController : ControllerBase
    {
        private readonly IAuditoriasRepository auditoriasRepository;
        private readonly ILogger<AuditoriasController> logger;

        public AuditoriasController(
            IAuditoriasRepository auditoriasRepository,
            ILogger<AuditoriasController> logger)
        {
            this.auditoriasRepository = auditoriasRepository;
            this.logger = logger;
        }

        [HttpGet("GetAuditorias")]
        [RequierePermiso(PermisosSistema.auditoriasConsultar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuditorias()
        {
            try
            {
                var response = await auditoriasRepository.GetAuditorias();

                if (response == null || response.Count == 0)
                {
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "No se encontró información de auditorías."
                    });
                }

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Consulta de auditorías realizada correctamente.",
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpGet("GetAuditoriaById/{id}")]
        [RequierePermiso(PermisosSistema.auditoriasConsultar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuditoriaById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "El identificador debe ser mayor que cero."
                    });

                var response = await auditoriasRepository.GetAuditoriasById(id);

                if (response == null)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "No se encontró la auditoría solicitada."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Auditoría encontrada correctamente.",
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPost("PostAuditoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostAuditoria([FromBody] AuditoriaCrearDTO dto)
        {
            try
            {
                var auditoria = dto.ToEntity();

                var idUsuarioActual = User.ObtenerIdUsuario();

                if (idUsuarioActual == null)
                {
                    return Unauthorized(new
                    {
                        statusCode = 401,
                        message = "No fue posible identificar al usuario autenticado."
                    });
                }

                auditoria.idUsuario = idUsuarioActual.Value;

                // La fecha la fija el servidor: la que envía el cliente se ignora para que
                // nadie pueda registrar auditorías con fechas falsas. El contrato no cambia.
                auditoria.fecha = DateTime.Now;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (auditoria == null)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Los datos de la auditoría son obligatorios."
                    });

                var response = await auditoriasRepository.PostAuditorias(auditoria);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "No fue posible registrar la auditoría."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Auditoría registrada correctamente.",
                    data = auditoria.ToRespuestaDTO()
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

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPut("PutAuditoria")]
        [RequierePermiso(PermisosSistema.auditoriasModificar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutAuditoria([FromBody] AuditoriaActualizarDTO auditoria)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (auditoria == null || auditoria.idAuditoria <= 0)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Los datos de la auditoría no son válidos."
                    });

                var existente = await auditoriasRepository.GetAuditoriasById(auditoria.idAuditoria);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "La auditoría que se desea actualizar no existe."
                    });

                existente.tabla = auditoria.tabla;
                existente.accion = auditoria.accion;
                existente.idUsuario = auditoria.idUsuario;
                existente.fecha = auditoria.fecha;
                existente.datosAnteriores = auditoria.datosAnteriores;
                existente.datosNuevos = auditoria.datosNuevos;

                var response = await auditoriasRepository.PutAuditorias(existente);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "No fue posible actualizar la auditoría."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Auditoría actualizada correctamente."
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
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpDelete("DeleteAuditoria/{id}")]
        [RequierePermiso(PermisosSistema.auditoriasModificar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAuditoria(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "El identificador debe ser mayor que cero."
                    });

                var auditoria = await auditoriasRepository.GetAuditoriasById(id);

                if (auditoria == null)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "La auditoría que se desea eliminar no existe."
                    });

                var response = await auditoriasRepository.DeleteAuditorias(auditoria);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "No fue posible eliminar la auditoría."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Auditoría eliminada correctamente."
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

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }
    }
}