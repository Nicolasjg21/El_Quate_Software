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
    public class PedidosController : ControllerBase
    {
        private readonly IPedidosRepository _pedidosRepository;

        public PedidosController(IPedidosRepository pedidosRepository)
        {
            _pedidosRepository = pedidosRepository;
        }

        [HttpGet("GetPedidos")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPedidos()
        {
            try
            {
                var response = await _pedidosRepository.GetPedidos();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de pedidos." });

                return Ok(new { statusCode = 200, message = "Consulta de pedidos realizada correctamente.", data = response });
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

        [HttpGet("GetPedidoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPedidoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _pedidosRepository.GetPedidosById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El pedido solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Pedido encontrado correctamente.", data = response });
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

        [HttpPost("PostPedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostPedido([FromBody] Pedidos pedidos)
        {
            try
            {
                if (!ModelState.IsValid || pedidos == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del pedido no son válidos." });

                var response = await _pedidosRepository.PostPedidos(pedidos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el pedido." });

                return Ok(new { statusCode = 200, message = "Pedido registrado correctamente." });
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

        [HttpPut("PutPedido")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutPedido([FromBody] Pedidos pedidos)
        {
            try
            {
                if (!ModelState.IsValid || pedidos == null || pedidos.idPedido <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del pedido no son válidos." });

                var existente = await _pedidosRepository.GetPedidosById(pedidos.idPedido);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El pedido que se desea actualizar no existe." });

                var response = await _pedidosRepository.PutPedidos(pedidos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el pedido." });

                return Ok(new { statusCode = 200, message = "Pedido actualizado correctamente." });
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

        [HttpDelete("DeletePedido/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeletePedido(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var pedido = await _pedidosRepository.GetPedidosById(id);

                if (pedido == null)
                    return NotFound(new { statusCode = 404, message = "El pedido que se desea eliminar no existe." });

                var response = await _pedidosRepository.DeletePedidos(pedido);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el pedido." });

                return Ok(new { statusCode = 200, message = "Pedido eliminado correctamente." });
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