using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Cuentas;
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

    public class CuentasController : ControllerBase
    {
        private readonly ICuentasRepository cuentasRepository;
        private readonly ILogger<CuentasController> logger;

        public CuentasController(
            ICuentasRepository cuentasRepository,
            ILogger<CuentasController> logger)
        {
            this.cuentasRepository = cuentasRepository;
            this.logger = logger;
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
                var response = await cuentasRepository.GetCuentas();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de cuentas." });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de cuentas realizada correctamente.",
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

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

                var response = await cuentasRepository.GetCuentasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La cuenta solicitada no existe." });

                return Ok(new { statusCode = 200, message = "Cuenta encontrada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPost("PostCuenta")]
        [RequierePermiso(PermisosSistema.cuentasGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCuenta([FromBody] CuentaCrearDTO dto)
        {
            try
            {
                var cuentas = dto.ToEntity();

                if (!ModelState.IsValid || cuentas == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos de la cuenta no son válidos." });

                var response = await cuentasRepository.PostCuentas(cuentas);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar la cuenta." });

                return Ok(new { statusCode = 200, message = "Cuenta registrada correctamente.",
                    data = cuentas.ToRespuestaDTO() });
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

        [HttpPut("PutCuenta")]
        [RequierePermiso(PermisosSistema.cuentasGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCuenta([FromBody] CuentaActualizarDTO cuentas)
        {
            try
            {
                if (!ModelState.IsValid || cuentas == null || cuentas.idCuenta <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la cuenta no son válidos."
                    });
                }

                var existente = await cuentasRepository
                    .GetCuentasById(cuentas.idCuenta);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La cuenta que se desea actualizar no existe."
                    });
                }

                existente.idMesa = cuentas.idMesa;
                existente.estado = cuentas.estado;
                existente.fechaApertura = cuentas.fechaApertura;
                existente.fechaCierre = cuentas.fechaCierre;
                existente.total = cuentas.total;

                var response = await cuentasRepository
                    .PutCuentas(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la cuenta."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Cuenta actualizada correctamente."
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

        [HttpDelete("DeleteCuenta/{id}")]
        [RequierePermiso(PermisosSistema.cuentasGestionar)]
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

                var cuenta = await cuentasRepository.GetCuentasById(id);

                if (cuenta == null)
                    return NotFound(new { statusCode = 404, message = "La cuenta que se desea eliminar no existe." });

                var response = await cuentasRepository.DeleteCuentas(cuenta);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la cuenta." });

                return Ok(new { statusCode = 200, message = "Cuenta eliminada correctamente." });
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