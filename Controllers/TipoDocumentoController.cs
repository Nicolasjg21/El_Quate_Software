using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class TipoDocumentoController : ControllerBase
    {
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;

        public TipoDocumentoController(ITipoDocumentoRepository tipoDocumentoRepository)
        {
            _tipoDocumentoRepository = tipoDocumentoRepository;
        }

        [HttpGet("GetTipoDocumentos")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTipoDocumentos()
        {
            try
            {
                var response = await _tipoDocumentoRepository.GetTipoDocumento();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de tipos de documento." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de tipos de documento realizada correctamente.",
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

        [HttpGet("GetTipoDocumentoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTipoDocumentoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _tipoDocumentoRepository.GetTipoDocumentoById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El tipo de documento solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Tipo de documento encontrado correctamente.", data = response });
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

        [HttpPost("PostTipoDocumento")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostTipoDocumento([FromBody] TipoDocumento tipoDocumento)
        {
            try
            {
                if (!ModelState.IsValid || tipoDocumento == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del tipo de documento no son válidos." });

                var response = await _tipoDocumentoRepository.PostTipoDocumento(tipoDocumento);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el tipo de documento." });

                return Ok(new { statusCode = 200, message = "Tipo de documento registrado correctamente." });
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

        [HttpPut("PutTipoDocumento")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutTipoDocumento([FromBody] TipoDocumento tipoDocumento)
        {
            try
            {
                if (!ModelState.IsValid || tipoDocumento == null || tipoDocumento.idTipoDocumento <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del tipo de documento no son válidos." });

                var existente = await _tipoDocumentoRepository.GetTipoDocumentoById(tipoDocumento.idTipoDocumento);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El tipo de documento que se desea actualizar no existe." });

                var response = await _tipoDocumentoRepository.PutTipoDocumento(tipoDocumento);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el tipo de documento." });

                return Ok(new { statusCode = 200, message = "Tipo de documento actualizado correctamente." });
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

        [HttpDelete("DeleteTipoDocumento/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteTipoDocumento(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var tipoDocumento = await _tipoDocumentoRepository.GetTipoDocumentoById(id);

                if (tipoDocumento == null)
                    return NotFound(new { statusCode = 404, message = "El tipo de documento que se desea eliminar no existe." });

                var response = await _tipoDocumentoRepository.DeleteTipoDocumento(tipoDocumento);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el tipo de documento." });

                return Ok(new { statusCode = 200, message = "Tipo de documento eliminado correctamente." });
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