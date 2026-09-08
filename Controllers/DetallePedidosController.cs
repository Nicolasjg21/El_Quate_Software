using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    [Authorize]

    public class DetallePedidosController : ControllerBase
    {
        private readonly IDetallePedidosRepository _detallePedidosRepository;

        public DetallePedidosController(IDetallePedidosRepository detallePedidosRepository)
        {
            _detallePedidosRepository = detallePedidosRepository;
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
                var response = await _detallePedidosRepository.GetDetallePedidos();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de detalles de pedidos." });

                return Ok(new { statusCode = 200, message = "Consulta realizada correctamente.", data = response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { statusCode = 404, message = ex.Message });
            }
            catch (Exception)
            {
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

                var response = await _detallePedidosRepository.GetDetallePedidosById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de pedido solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Detalle de pedido encontrado correctamente.", data = response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { statusCode = 404, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostDetallePedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostDetallePedido([FromBody] DetallePedidos detallePedidos)
        {
            try
            {
                if (!ModelState.IsValid || detallePedidos == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del detalle de pedido no son válidos." });

                var response = await _detallePedidosRepository.PostDetallePedidos(detallePedidos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el detalle de pedido." });

                return Ok(new { statusCode = 200, message = "Detalle de pedido registrado correctamente." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPut("PutDetallePedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutDetallePedido([FromBody] DetallePedidos detallePedidos)
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

                var existente = await _detallePedidosRepository
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

                var response = await _detallePedidosRepository
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
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = 401,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
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

                var detalle = await _detallePedidosRepository.GetDetallePedidosById(id);

                if (detalle == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de pedido que se desea eliminar no existe." });

                var response = await _detallePedidosRepository.DeleteDetallePedidos(detalle);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el detalle de pedido." });

                return Ok(new { statusCode = 200, message = "Detalle de pedido eliminado correctamente." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }
    }
}