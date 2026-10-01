using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.DetalleCompras;
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

    public class DetalleComprasController : ControllerBase
    {
        private readonly IDetalleComprasRepository detalleComprasRepository;
        private readonly ILogger<DetalleComprasController> logger;

        public DetalleComprasController(
            IDetalleComprasRepository detalleComprasRepository,
            ILogger<DetalleComprasController> logger)
        {
            this.detalleComprasRepository = detalleComprasRepository;
            this.logger = logger;
        }

        [HttpGet("GetDetalleCompras")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDetalleCompras()
        {
            try
            {
                var response = await detalleComprasRepository.GetDetalleCompras();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de detalles de compras." });

                return Ok(new { statusCode = 200, message = "Consulta realizada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("GetDetalleCompraById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDetalleCompraById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await detalleComprasRepository.GetDetalleComprasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de compra solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Detalle de compra encontrado correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostDetalleCompra")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostDetalleCompra([FromBody] DetalleCompraCrearDTO dto)
        {
            try
            {
                var detalleCompras = dto.ToEntity();

                if (!ModelState.IsValid || detalleCompras == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del detalle de compra no son válidos." });

                var response = await detalleComprasRepository.PostDetalleCompras(detalleCompras);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el detalle de compra." });

                return Ok(new { statusCode = 200, message = "Detalle de compra registrado correctamente.",
                    data = detalleCompras.ToRespuestaDTO() });
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

        [HttpPut("PutDetalleCompra")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutDetalleCompra([FromBody] DetalleCompraActualizarDTO detalleCompras)
        {
            try
            {
                if (!ModelState.IsValid || detalleCompras == null || detalleCompras.idDetalleCompra <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del detalle de compra no son válidos."
                    });
                }

                var existente = await detalleComprasRepository
                    .GetDetalleComprasById(detalleCompras.idDetalleCompra);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El detalle de compra que se desea actualizar no existe."
                    });
                }

                existente.idCompra = detalleCompras.idCompra;
                existente.idProducto = detalleCompras.idProducto;
                existente.cantidad = detalleCompras.cantidad;
                existente.precioCompra = detalleCompras.precioCompra;

                var response = await detalleComprasRepository
                    .PutDetalleCompras(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el detalle de compra."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Detalle de compra actualizado correctamente."
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

        [HttpDelete("DeleteDetalleCompra/{id}")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDetalleCompra(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var detalle = await detalleComprasRepository.GetDetalleComprasById(id);

                if (detalle == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de compra que se desea eliminar no existe." });

                var response = await detalleComprasRepository.DeleteDetalleCompras(detalle);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el detalle de compra." });

                return Ok(new { statusCode = 200, message = "Detalle de compra eliminado correctamente." });
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