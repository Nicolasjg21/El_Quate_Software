using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobantesRepository comprobantesRepository;
        private readonly ILogger<ComprobantesController> logger;

        public ComprobantesController(
            IComprobantesRepository comprobantesRepository,
            ILogger<ComprobantesController> logger)
        {
            this.comprobantesRepository = comprobantesRepository;
            this.logger = logger;
        }

        [HttpGet("GetComprobantes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetComprobantes()
        {
            try
            {
                var response = await comprobantesRepository.GetComprobantes();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de comprobantes." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de comprobantes realizada correctamente.",
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("GetComprobanteById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetComprobanteById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await comprobantesRepository.GetComprobantesById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El comprobante solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Comprobante encontrado correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostComprobante")]
        [RequierePermiso(PermisosSistema.cobrosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostComprobante([FromBody] ComprobanteCrearDTO dto)
        {
            try
            {
                var comprobantes = dto.ToEntity();

                if (!ModelState.IsValid || comprobantes == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del comprobante no son válidos." });

                var response = await comprobantesRepository.PostComprobantes(comprobantes);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el comprobante." });

                return Ok(new { statusCode = 200, message = "Comprobante registrado correctamente.",
                    data = comprobantes.ToRespuestaDTO() });
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

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPut("PutComprobante")]
        [RequierePermiso(PermisosSistema.cobrosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutComprobante([FromBody] ComprobanteActualizarDTO comprobantes)
        {
            try
            {
                if (!ModelState.IsValid || comprobantes == null || comprobantes.idComprobante <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del comprobante no son válidos."
                    });

                var existente = await comprobantesRepository
                    .GetComprobantesById(comprobantes.idComprobante);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El comprobante que se desea actualizar no existe."
                    });

                existente.idCuenta = comprobantes.idCuenta;
                existente.fecha = comprobantes.fecha;
                existente.total = comprobantes.total;
                existente.idMetodo = comprobantes.idMetodo;

                var response = await comprobantesRepository
                    .PutComprobantes(existente);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el comprobante."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Comprobante actualizado correctamente."
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

        [HttpDelete("DeleteComprobante/{id}")]
        [RequierePermiso(PermisosSistema.cobrosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteComprobante(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var comprobante = await comprobantesRepository.GetComprobantesById(id);

                if (comprobante == null)
                    return NotFound(new { statusCode = 404, message = "El comprobante que se desea eliminar no existe." });

                var response = await comprobantesRepository.DeleteComprobantes(comprobante);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el comprobante." });

                return Ok(new { statusCode = 200, message = "Comprobante eliminado correctamente." });
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

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }
    }
}