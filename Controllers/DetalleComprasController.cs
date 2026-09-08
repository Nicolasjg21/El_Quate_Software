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

    public class DetalleComprasController : ControllerBase
    {
        private readonly IDetalleComprasRepository _detalleComprasRepository;

        public DetalleComprasController(IDetalleComprasRepository detalleComprasRepository)
        {
            _detalleComprasRepository = detalleComprasRepository;
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
                var response = await _detalleComprasRepository.GetDetalleCompras();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de detalles de compras." });

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

                var response = await _detalleComprasRepository.GetDetalleComprasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de compra solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Detalle de compra encontrado correctamente.", data = response });
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

        [HttpPost("PostDetalleCompra")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostDetalleCompra([FromBody] DetalleCompras detalleCompras)
        {
            try
            {
                if (!ModelState.IsValid || detalleCompras == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del detalle de compra no son válidos." });

                var response = await _detalleComprasRepository.PostDetalleCompras(detalleCompras);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el detalle de compra." });

                return Ok(new { statusCode = 200, message = "Detalle de compra registrado correctamente." });
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

        [HttpPut("PutDetalleCompra")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutDetalleCompra([FromBody] DetalleCompras detalleCompras)
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

                var existente = await _detalleComprasRepository
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

                var response = await _detalleComprasRepository
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

        [HttpDelete("DeleteDetalleCompra/{id}")]
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

                var detalle = await _detalleComprasRepository.GetDetalleComprasById(id);

                if (detalle == null)
                    return NotFound(new { statusCode = 404, message = "El detalle de compra que se desea eliminar no existe." });

                var response = await _detalleComprasRepository.DeleteDetalleCompras(detalle);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el detalle de compra." });

                return Ok(new { statusCode = 200, message = "Detalle de compra eliminado correctamente." });
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