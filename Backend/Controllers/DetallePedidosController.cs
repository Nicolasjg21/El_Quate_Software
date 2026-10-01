using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.DetallePedidos;
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

    public class DetallePedidosController : ControllerBase
    {
        private readonly IDetallePedidosRepository detallePedidosRepository;
        private readonly ILogger<DetallePedidosController> logger;

        public DetallePedidosController(
            IDetallePedidosRepository detallePedidosRepository,
            ILogger<DetallePedidosController> logger)
        {
            this.detallePedidosRepository = detallePedidosRepository;
            this.logger = logger;
        }

        [HttpGet("GetDetallePedidos")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDetallePedidos()
        {
            try
            {
                var response = await detallePedidosRepository.GetDetallePedidos();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de detalles de pedidos." });

                return Ok(new { statusCode = 200, message = "Consulta realizada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("GetDetallePedidoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDetallePedidoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await detallePedidosRepository.GetDetallePedidosById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de pedido solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Detalle de pedido encontrado correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostDetallePedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostDetallePedido([FromBody] DetallePedidoCrearDTO dto)
        {
            try
            {
                var detallePedidos = dto.ToEntity();

                if (!ModelState.IsValid || detallePedidos == null)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del detalle de pedido no son válidos."
                    });
                }

                var response = await detallePedidosRepository
                    .PostDetallePedidos(detallePedidos);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el detalle de pedido."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Detalle de pedido registrado correctamente.",
                    data = detallePedidos.ToRespuestaDTO()
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

        [HttpPut("PutDetallePedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutDetallePedido([FromBody] DetallePedidoActualizarDTO detallePedidos)
        {
            try
            {
                if (!ModelState.IsValid || detallePedidos == null || detallePedidos.idDetalle <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del detalle de pedido no son válidos."
                    });
                }

                var existente = await detallePedidosRepository
                    .GetDetallePedidosById(detallePedidos.idDetalle);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El detalle de pedido que se desea actualizar no existe."
                    });
                }

                existente.idPedido = detallePedidos.idPedido;
                existente.idProducto = detallePedidos.idProducto;
                existente.cantidad = detallePedidos.cantidad;
                existente.precioUnitario = detallePedidos.precioUnitario;

                var response = await detallePedidosRepository
                    .PutDetallePedidos(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el detalle de pedido."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Detalle de pedido actualizado correctamente."
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

        [HttpDelete("DeleteDetallePedido/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteDetallePedido(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var detalle = await detallePedidosRepository.GetDetallePedidosById(id);

                if (detalle == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de pedido que se desea eliminar no existe." });

                var response = await detallePedidosRepository.DeleteDetallePedidos(detalle);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el detalle de pedido." });

                return Ok(new { statusCode = 200, message = "Detalle de pedido eliminado correctamente." });
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