using ElQuateDePatty.Models;
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
        private readonly IAuditoriasRepository _auditoriasRepository;

        public AuditoriasController(IAuditoriasRepository auditoriasRepository)
        {
            _auditoriasRepository = auditoriasRepository;
        }

        [HttpGet("GetAuditorias")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuditorias()
        {
            try
            {
                var response = await _auditoriasRepository.GetAuditorias();

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
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = StatusCodes.Status404NotFound,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpGet("GetAuditoriaById/{id}")]
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

                var response = await _auditoriasRepository.GetAuditoriasById(id);

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
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = StatusCodes.Status404NotFound,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
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
        public async Task<IActionResult> PostAuditoria([FromBody] Auditorias auditoria)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (auditoria == null)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "Los datos de la auditoría son obligatorios."
                    });

                var response = await _auditoriasRepository.PostAuditorias(auditoria);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "No fue posible registrar la auditoría."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Auditoría registrada correctamente."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPut("PutAuditoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutAuditoria([FromBody] Auditorias auditoria)
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

                var existente = await _auditoriasRepository.GetAuditoriasById(auditoria.idAuditoria);

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

                var response = await _auditoriasRepository.PutAuditorias(existente);

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
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }

        [HttpDelete("DeleteAuditoria/{id}")]
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

                var auditoria = await _auditoriasRepository.GetAuditoriasById(id);

                if (auditoria == null)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "La auditoría que se desea eliminar no existe."
                    });

                var response = await _auditoriasRepository.DeleteAuditorias(auditoria);

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
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = StatusCodes.Status404NotFound,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }
    }
}