using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Compras;
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

    public class ComprasController : ControllerBase
    {
        private readonly IComprasRepository comprasRepository;
        private readonly ILogger<ComprasController> logger;

        public ComprasController(
            IComprasRepository comprasRepository,
            ILogger<ComprasController> logger)
        {
            this.comprasRepository = comprasRepository;
            this.logger = logger;
        }

        [HttpGet("GetCompras")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompras()
        {
            try
            {
                var response = await comprasRepository.GetCompras();

                if (response == null || response.Count == 0)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró información de compras."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de compras realizada correctamente.",
                    data = response.ToRespuestaDTO()
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

        [HttpGet("GetCompraById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompraById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await comprasRepository.GetComprasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La compra solicitada no existe." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Compra encontrada correctamente.",
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostCompra")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCompra([FromBody] CompraCrearDTO dto)
        {
            try
            {
                var compras = dto.ToEntity();

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (compras == null)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la compra son obligatorios."
                    });

                var response = await comprasRepository.PostCompras(compras);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar la compra."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Compra registrada correctamente.",
                    data = compras.ToRespuestaDTO()
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

        [HttpPut("PutCompra")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCompra([FromBody] CompraActualizarDTO compras)
        {
            try
            {
                if (!ModelState.IsValid || compras == null || compras.idCompra <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la compra no son válidos."
                    });

                var existente = await comprasRepository.GetComprasById(compras.idCompra);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La compra que se desea actualizar no existe."
                    });

                existente.idProveedor = compras.idProveedor;
                existente.fecha = compras.fecha;
                existente.total = compras.total;

                var response = await comprasRepository.PutCompras(existente);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la compra."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Compra actualizada correctamente."
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

        [HttpDelete("DeleteCompra/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCompra(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var compra = await comprasRepository.GetComprasById(id);

                if (compra == null)
                    return NotFound(new { statusCode = 404, message = "La compra que se desea eliminar no existe." });

                var response = await comprasRepository.DeleteCompras(compra);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la compra." });

                return Ok(new { statusCode = 200, message = "Compra eliminada correctamente." });
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

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarCompras([FromQuery] CompraFiltroDTO filtro)
        {
            var data = await comprasRepository.FiltrarCompras(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Compras filtradas correctamente",
                data = data.ToRespuestaDTO()
            });
        }

        [HttpGet("Historial")]
        public async Task<IActionResult> FiltrarHistorialCompras([FromQuery] PeriodoFiltroDTO filtro)
        {
            var data = await comprasRepository.FiltrarHistorialCompras(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Historial de compras consultado correctamente",
                data = data.ToRespuestaDTO()
            });
        }
    }
}