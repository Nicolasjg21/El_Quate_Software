using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.TipoDocumento;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TipoDocumentoController : ControllerBase
    {
        private readonly ITipoDocumentoRepository tipoDocumentoRepository;
        private readonly ILogger<TipoDocumentoController> logger;

        public TipoDocumentoController(
            ITipoDocumentoRepository repository,
            ILogger<TipoDocumentoController> logger)
        {
            tipoDocumentoRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetTipoDocumento")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTipoDocumento()
        {
            try
            {
                var tipos = await tipoDocumentoRepository.GetTipoDocumento();

                if (tipos == null || !tipos.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron tipos de documento."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Tipos de documento obtenidos correctamente.",
                    data = tipos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los tipos de documento."
                });
            }
        }

        [HttpGet("GetTipoDocumentoById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTipoDocumentoById(int id)
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

                var tipo = await tipoDocumentoRepository.GetTipoDocumentoById(id);

                if (tipo == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el tipo de documento."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Tipo de documento obtenido correctamente.",
                    data = tipo.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el tipo de documento."
                });
            }
        }

        [HttpPost("PostTipoDocumento")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostTipoDocumento([FromBody] TipoDocumentoCrearDTO dto)
        {
            try
            {
                var tipo = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                var response = await tipoDocumentoRepository.PostTipoDocumento(tipo);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el tipo de documento."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Tipo de documento registrado correctamente.",
                    data = tipo.ToRespuestaDTO()
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
                    message = "Ocurrió un error interno al registrar el tipo de documento."
                });
            }
        }

        [HttpPut("PutTipoDocumento")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutTipoDocumento([FromBody] TipoDocumentoActualizarDTO tipo)
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

                if (tipo.idTipoDocumento <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await tipoDocumentoRepository.GetTipoDocumentoById(tipo.idTipoDocumento);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el tipo de documento que desea actualizar."
                    });
                }

                existente.nombreTipo = tipo.nombreTipo;

                var response = await tipoDocumentoRepository.PutTipoDocumento(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el tipo de documento."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Tipo de documento actualizado correctamente.",
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
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpDelete("DeleteTipoDocumento/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTipoDocumento(int id)
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

                var tipo = await tipoDocumentoRepository.GetTipoDocumentoById(id);

                if (tipo == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el tipo de documento."
                    });
                }

                var response = await tipoDocumentoRepository.DeleteTipoDocumento(tipo);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el tipo de documento."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Tipo de documento eliminado correctamente."
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
                    message = "Ocurrió un error interno al eliminar el tipo de documento."
                });
            }
        }
    }
}