using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobantesRepository _comprobantesRepository;

        public ComprobantesController(IComprobantesRepository comprobantesRepository)
        {
            _comprobantesRepository = comprobantesRepository;
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
                var response = await _comprobantesRepository.GetComprobantes();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de comprobantes." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de comprobantes realizada correctamente.",
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

                var response = await _comprobantesRepository.GetComprobantesById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El comprobante solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Comprobante encontrado correctamente.", data = response });
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

        [HttpPost("PostComprobante")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostComprobante([FromBody] Comprobantes comprobantes)
        {
            try
            {
                if (!ModelState.IsValid || comprobantes == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del comprobante no son válidos." });

                var response = await _comprobantesRepository.PostComprobantes(comprobantes);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el comprobante." });

                return Ok(new { statusCode = 200, message = "Comprobante registrado correctamente." });
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

        [HttpPut("PutComprobante")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutComprobante([FromBody] Comprobantes comprobantes)
        {
            try
            {
                if (!ModelState.IsValid || comprobantes == null || comprobantes.idComprobante <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del comprobante no son válidos."
                    });

                var existente = await _comprobantesRepository
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

                var response = await _comprobantesRepository
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }

        [HttpDelete("DeleteComprobante/{id}")]
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

                var comprobante = await _comprobantesRepository.GetComprobantesById(id);

                if (comprobante == null)
                    return NotFound(new { statusCode = 404, message = "El comprobante que se desea eliminar no existe." });

                var response = await _comprobantesRepository.DeleteComprobantes(comprobante);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el comprobante." });

                return Ok(new { statusCode = 200, message = "Comprobante eliminado correctamente." });
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