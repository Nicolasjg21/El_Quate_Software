using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
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

        /// <summary>
        /// Lista las cuentas. El parámetro opcional <paramref name="estado"/> (por ejemplo, Abierta)
        /// limita el resultado a ese estado; sin él se devuelven todas las cuentas, como antes.
        /// </summary>
        [HttpGet("GetCuentas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCuentas([FromQuery] string? estado = null)
        {
            try
            {
                var response = await cuentasRepository.GetCuentas(estado);

                // Sin registros es un resultado válido: 200 con lista vacía (no 404).
                response ??= [];

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

                return Ok(new
                {
                    statusCode = 200,
                    message = "Cuenta registrada correctamente.",
                    data = cuentas.ToRespuestaDTO()
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

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpPut("PutCuenta")]
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

        /// <summary>
        /// Cobra una cuenta en una sola transacción del servidor: emite el comprobante (con base e INC),
        /// cierra la cuenta, marca los pedidos como pagados, libera la mesa y registra la auditoría.
        /// Si algo falla no queda nada a medias, y una cuenta nunca se cobra dos veces.
        /// <list type="bullet">
        /// <item>200: cobrada; data trae el comprobante.</item>
        /// <item>409 CUENTA_YA_COBRADA: ya tenía comprobante; data trae el existente (un reintento
        /// después de un corte de red llega aquí y no duplica el pago).</item>
        /// <item>409 TOTAL_DESACTUALIZADO: la cuenta cambió mientras se cobraba; totalActual trae el total vigente.</item>
        /// <item>409 CUENTA_NO_ABIERTA, 404 cuenta inexistente, 400 sin productos o método inexistente.</item>
        /// </list>
        /// </summary>
        [HttpPost("Cobrar")]
        [RequierePermiso(PermisosSistema.cobrosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cobrar([FromBody] CobrarCuentaDTO dto)
        {
            try
            {
                var idUsuario = User.ObtenerIdUsuario();

                if (idUsuario == null)
                    return Unauthorized(new { statusCode = 401, message = "No fue posible identificar al usuario autenticado." });

                if (!ModelState.IsValid || dto == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del cobro no son válidos." });

                var resultado = await cuentasRepository.Cobrar(dto.idCuenta, dto.idMetodo, dto.totalEsperado, idUsuario.Value);

                switch (resultado.estado)
                {
                    case EstadoCobro.Cobrada:
                        return Ok(new
                        {
                            statusCode = 200,
                            message = "Pago registrado. La cuenta quedó cerrada y la mesa libre.",
                            data = resultado.ToCobroRespuestaDTO()
                        });

                    case EstadoCobro.YaCobrada:
                        return Conflict(new
                        {
                            statusCode = 409,
                            codigo = "CUENTA_YA_COBRADA",
                            message = "Esta cuenta ya fue cobrada; no se registró un segundo pago.",
                            data = resultado.ToCobroRespuestaDTO()
                        });

                    case EstadoCobro.TotalDesactualizado:
                        return Conflict(new
                        {
                            statusCode = 409,
                            codigo = "TOTAL_DESACTUALIZADO",
                            message = "La cuenta cambió mientras se cobraba. Revise los productos y vuelva a cobrar.",
                            totalActual = resultado.totalActual
                        });

                    case EstadoCobro.CuentaNoAbierta:
                        return Conflict(new
                        {
                            statusCode = 409,
                            codigo = "CUENTA_NO_ABIERTA",
                            message = "La cuenta no está abierta y no se puede cobrar."
                        });

                    case EstadoCobro.CuentaNoExiste:
                        return NotFound(new { statusCode = 404, message = "La cuenta que se desea cobrar no existe." });

                    case EstadoCobro.MetodoNoExiste:
                        return BadRequest(new { statusCode = 400, message = "El método de pago seleccionado no existe." });

                    case EstadoCobro.SinProductos:
                        return BadRequest(new { statusCode = 400, message = "La cuenta no tiene productos para cobrar." });

                    default:
                        logger.LogError("Resultado de cobro no contemplado: {Estado}", resultado.estado);
                        return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
                }
            }
            catch (DbUpdateException ex)
            {
                // Lo más probable: el índice único rechazó un segundo comprobante para la cuenta.
                logger.LogWarning(ex, "Conflicto de integridad en {Metodo} {Ruta}", Request.Method, Request.Path);

                return Conflict(new
                {
                    statusCode = 409,
                    codigo = "CONFLICTO_COBRO",
                    message = "No se registró el pago porque la cuenta cambió o ya fue cobrada. Recargue la mesa y verifique."
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpDelete("DeleteCuenta/{id}")]
        [RequierePermiso(PermisosSistema.seguridadGestionar)]
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