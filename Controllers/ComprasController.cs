using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class ComprasController : ControllerBase
    {
        private readonly IComprasRepository _comprasRepository;

        public ComprasController(IComprasRepository comprasRepository)
        {
            _comprasRepository = comprasRepository;
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
                var response = await _comprasRepository.GetCompras();

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

                var response = await _comprasRepository.GetComprasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La compra solicitada no existe." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Compra encontrada correctamente.",
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

        [HttpPost("PostCompra")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCompra([FromBody] Compras compras)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (compras == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos de la compra son obligatorios." });

                var response = await _comprasRepository.PostCompras(compras);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar la compra." });

                return Ok(new { statusCode = 200, message = "Compra registrada correctamente." });
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

        [HttpPut("PutCompra")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCompra([FromBody] Compras compras)
        {
            try
            {
                if (!ModelState.IsValid || compras == null || compras.idCompra <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos de la compra no son válidos." });

                var existente = await _comprasRepository.GetComprasById(compras.idCompra);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "La compra que se desea actualizar no existe." });

                var response = await _comprasRepository.PutCompras(compras);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar la compra." });

                return Ok(new { statusCode = 200, message = "Compra actualizada correctamente." });
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

                var compra = await _comprasRepository.GetComprasById(id);

                if (compra == null)
                    return NotFound(new { statusCode = 404, message = "La compra que se desea eliminar no existe." });

                var response = await _comprasRepository.DeleteCompras(compra);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la compra." });

                return Ok(new { statusCode = 200, message = "Compra eliminada correctamente." });
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
    }
}