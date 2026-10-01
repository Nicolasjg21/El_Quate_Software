using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.MetodosPago;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MetodosPagoController : ControllerBase
    {
        private readonly IMetodosPagoRepository metodosPagoRepository;
        private readonly ILogger<MetodosPagoController> logger;

        public MetodosPagoController(
            IMetodosPagoRepository repository,
            ILogger<MetodosPagoController> logger)
        {
            metodosPagoRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetMetodosPago")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMetodosPago()
        {
            try
            {
                var metodos = await metodosPagoRepository.GetMetodosPago();

                if (metodos == null || !metodos.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron métodos de pago."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Métodos de pago obtenidos correctamente.",
                    data = metodos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los métodos de pago."
                });
            }
        }

        [HttpGet("GetMetodosPagoById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMetodosPagoById(int id)
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

                var metodo = await metodosPagoRepository.GetMetodosPagoById(id);

                if (metodo == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el método de pago."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Método de pago obtenido correctamente.",
                    data = metodo.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el método de pago."
                });
            }
        }

        [HttpPost("PostMetodosPago")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostMetodosPago([FromBody] MetodoPagoCrearDTO dto)
        {
            try
            {
                var metodo = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                var response = await metodosPagoRepository.PostMetodosPago(metodo);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el método de pago."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Método de pago registrado correctamente.",
                    data = metodo.ToRespuestaDTO()
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
                    message = "Ocurrió un error interno al registrar el método de pago."
                });
            }
        }

        [HttpPut("PutMetodosPago")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutMetodosPago([FromBody] MetodoPagoActualizarDTO metodo)
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

                if (metodo.idMetodo <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await metodosPagoRepository.GetMetodosPagoById(metodo.idMetodo);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el método de pago que desea actualizar."
                    });
                }

                existente.nombreMetodo = metodo.nombreMetodo;

                var response = await metodosPagoRepository.PutMetodosPago(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el método de pago."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Método de pago actualizado correctamente.",
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

        [HttpDelete("DeleteMetodosPago/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMetodosPago(int id)
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

                var metodo = await metodosPagoRepository.GetMetodosPagoById(id);

                if (metodo == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el método de pago."
                    });
                }

                var response = await metodosPagoRepository.DeleteMetodosPago(metodo);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el método de pago."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Método de pago eliminado correctamente."
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
                    message = "Ocurrió un error interno al eliminar el método de pago."
                });
            }
        }
    }
}