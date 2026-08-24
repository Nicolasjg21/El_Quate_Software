using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class KardexController : ControllerBase
    {
        private readonly IKardexRepository _kardexRepository;

        public KardexController(IKardexRepository kardexRepository)
        {
            _kardexRepository = kardexRepository;
        }

        [HttpGet("GetKardex")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKardex()
        {
            try
            {
                var response = await _kardexRepository.GetKardex();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información del kardex." });

                return Ok(new { statusCode = 200, message = "Consulta de kardex realizada correctamente.", data = response });
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

        [HttpGet("GetKardexById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKardexById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _kardexRepository.GetKardexById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El movimiento solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Movimiento encontrado correctamente.", data = response });
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

        [HttpPost("PostKardex")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostKardex([FromBody] Kardex kardex)
        {
            try
            {
                if (!ModelState.IsValid || kardex == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del movimiento no son válidos." });

                var response = await _kardexRepository.PostKardex(kardex);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el movimiento." });

                return Ok(new { statusCode = 200, message = "Movimiento registrado correctamente." });
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

        [HttpPut("PutKardex")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutKardex([FromBody] Kardex kardex)
        {
            try
            {
                if (!ModelState.IsValid || kardex == null || kardex.idMovimiento <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del movimiento no son válidos." });

                var existente = await _kardexRepository.GetKardexById(kardex.idMovimiento);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El movimiento que se desea actualizar no existe." });

                var response = await _kardexRepository.PutKardex(kardex);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el movimiento." });

                return Ok(new { statusCode = 200, message = "Movimiento actualizado correctamente." });
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

        [HttpDelete("DeleteKardex/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteKardex(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var kardex = await _kardexRepository.GetKardexById(id);

                if (kardex == null)
                    return NotFound(new { statusCode = 404, message = "El movimiento que se desea eliminar no existe." });

                var response = await _kardexRepository.DeleteKardex(kardex);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el movimiento." });

                return Ok(new { statusCode = 200, message = "Movimiento eliminado correctamente." });
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