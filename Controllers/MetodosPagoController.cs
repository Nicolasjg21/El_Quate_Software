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
    public class MetodosPagoController : ControllerBase
    {
        private readonly IMetodosPagoRepository _metodosPagoRepository;

        public MetodosPagoController(IMetodosPagoRepository metodosPagoRepository)
        {
            _metodosPagoRepository = metodosPagoRepository;
        }

        [HttpGet("GetMetodosPago")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMetodosPago()
        {
            try
            {
                var response = await _metodosPagoRepository.GetMetodosPago();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de métodos de pago." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta realizada correctamente.",
                    data = response
                });
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

        [HttpGet("GetMetodoPagoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMetodoPagoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _metodosPagoRepository.GetMetodosPagoById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El método de pago solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Método de pago encontrado correctamente.", data = response });
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

        [HttpPost("PostMetodoPago")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostMetodoPago([FromBody] MetodosPago metodosPago)
        {
            try
            {
                if (!ModelState.IsValid || metodosPago == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del método de pago no son válidos." });

                var response = await _metodosPagoRepository.PostMetodosPago(metodosPago);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el método de pago." });

                return Ok(new { statusCode = 200, message = "Método de pago registrado correctamente." });
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

        [HttpPut("PutMetodoPago")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutMetodoPago([FromBody] MetodosPago metodosPago)
        {
            try
            {
                if (!ModelState.IsValid || metodosPago == null || metodosPago.idMetodo <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del método de pago no son válidos." });

                var existente = await _metodosPagoRepository.GetMetodosPagoById(metodosPago.idMetodo);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El método de pago que se desea actualizar no existe." });

                var response = await _metodosPagoRepository.PutMetodosPago(metodosPago);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el método de pago." });

                return Ok(new { statusCode = 200, message = "Método de pago actualizado correctamente." });
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

        [HttpDelete("DeleteMetodoPago/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteMetodoPago(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var metodo = await _metodosPagoRepository.GetMetodosPagoById(id);

                if (metodo == null)
                    return NotFound(new { statusCode = 404, message = "El método de pago que se desea eliminar no existe." });

                var response = await _metodosPagoRepository.DeleteMetodosPago(metodo);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el método de pago." });

                return Ok(new { statusCode = 200, message = "Método de pago eliminado correctamente." });
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