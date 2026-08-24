using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class CuentasController : ControllerBase
    {
        private readonly ICuentasRepository _cuentasRepository;

        public CuentasController(ICuentasRepository cuentasRepository)
        {
            _cuentasRepository = cuentasRepository;
        }

        [HttpGet("GetCuentas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCuentas()
        {
            try
            {
                var response = await _cuentasRepository.GetCuentas();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de cuentas." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de cuentas realizada correctamente.",
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

        [HttpGet("GetCuentaById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCuentaById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _cuentasRepository.GetCuentasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La cuenta solicitada no existe." });

                return Ok(new { statusCode = 200, message = "Cuenta encontrada correctamente.", data = response });
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

        [HttpPost("PostCuenta")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCuenta([FromBody] Cuentas cuentas)
        {
            try
            {
                if (!ModelState.IsValid || cuentas == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos de la cuenta no son válidos." });

                var response = await _cuentasRepository.PostCuentas(cuentas);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar la cuenta." });

                return Ok(new { statusCode = 200, message = "Cuenta registrada correctamente." });
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

        [HttpPut("PutCuenta")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCuenta([FromBody] Cuentas cuentas)
        {
            try
            {
                if (!ModelState.IsValid || cuentas == null || cuentas.idCuenta <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos de la cuenta no son válidos." });

                var existente = await _cuentasRepository.GetCuentasById(cuentas.idCuenta);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "La cuenta que se desea actualizar no existe." });

                var response = await _cuentasRepository.PutCuentas(cuentas);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar la cuenta." });

                return Ok(new { statusCode = 200, message = "Cuenta actualizada correctamente." });
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

        [HttpDelete("DeleteCuenta/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCuenta(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var cuenta = await _cuentasRepository.GetCuentasById(id);

                if (cuenta == null)
                    return NotFound(new { statusCode = 404, message = "La cuenta que se desea eliminar no existe." });

                var response = await _cuentasRepository.DeleteCuentas(cuenta);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la cuenta." });

                return Ok(new { statusCode = 200, message = "Cuenta eliminada correctamente." });
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