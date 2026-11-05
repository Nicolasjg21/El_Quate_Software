using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Kardex;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Services;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class KardexController : ControllerBase
    {
        private readonly IKardexRepository kardexRepository;
        private readonly IAuthorizationService authorizationService;
        private readonly ILogger<KardexController> logger;

        public KardexController(
            IKardexRepository kardexRepository,
            IAuthorizationService authorizationService,
            ILogger<KardexController> logger)
        {
            this.kardexRepository = kardexRepository;
            this.authorizationService = authorizationService;
            this.logger = logger;
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
                var response = await kardexRepository.GetKardex();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información del kardex." });

                return Ok(new { statusCode = 200, message = "Consulta de kardex realizada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

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

                var response = await kardexRepository.GetKardexById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El movimiento solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Movimiento encontrado correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        /// <summary>
        /// Registra un movimiento de inventario. El servidor calcula stockAnterior, stockNuevo y fecha
        /// a partir del último movimiento del producto; los valores enviados en esos campos se ignoran,
        /// salvo stockNuevo en un AJUSTE, que exige el permiso inventario.gestionar.
        /// Responde 409 si una salida supera el stock disponible.
        /// </summary>
        [HttpPost("PostKardex")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostKardex([FromBody] KardexCrearDTO dto)
        {
            try
            {
                var idUsuarioActual = User.ObtenerIdUsuario();

                if (idUsuarioActual == null)
                {
                    return Unauthorized(new
                    {
                        statusCode = 401,
                        message = "No fue posible identificar al usuario autenticado."
                    });
                }

                if (!ModelState.IsValid || dto == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del movimiento no son válidos." });

                var tipo = (dto.tipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();

                if (tipo != "ENTRADA" && tipo != "SALIDA" && tipo != "AJUSTE")
                    return BadRequest(new { statusCode = 400, message = "El tipo de movimiento debe ser ENTRADA, SALIDA o AJUSTE." });

                if (tipo == "AJUSTE")
                {
                    // Fijar el stock a un valor exacto es una operación administrativa.
                    var autorizado = (await authorizationService.AuthorizeAsync(
                        User, PoliticasAutorizacion.Permiso(PermisosSistema.inventarioGestionar))).Succeeded;

                    if (!autorizado)
                        return StatusCode(403, new { statusCode = 403, message = "No tiene permiso para ajustar el inventario." });

                    if (dto.stockNuevo < 0)
                        return BadRequest(new { statusCode = 400, message = "El stock ajustado no puede ser negativo." });
                }
                else if (dto.cantidad <= 0)
                {
                    return BadRequest(new { statusCode = 400, message = "La cantidad del movimiento debe ser mayor que cero." });
                }

                var kardex = dto.ToEntity();
                kardex.tipoMovimiento = tipo;
                kardex.idUsuario = idUsuarioActual.Value;

                // stockAnterior, stockNuevo (salvo en AJUSTE) y fecha se calculan en el servidor.
                var (registrado, stock) = await kardexRepository.RegistrarMovimiento(kardex);

                if (!registrado)
                {
                    return Conflict(new
                    {
                        statusCode = 409,
                        message = $"Stock insuficiente: hay {stock} unidad(es) disponible(s)."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Movimiento registrado correctamente.",
                    data = kardex.ToRespuestaDTO()
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

        [HttpPut("PutKardex")]
        [RequierePermiso(PermisosSistema.kardexModificar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutKardex([FromBody] KardexActualizarDTO kardex)
        {
            try
            {
                if (!ModelState.IsValid || kardex == null || kardex.idMovimiento <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del movimiento no son válidos."
                    });
                }

                var existente = await kardexRepository
                    .GetKardexById(kardex.idMovimiento);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El movimiento que se desea actualizar no existe."
                    });
                }

                existente.idProducto = kardex.idProducto;
                existente.tipoMovimiento = kardex.tipoMovimiento;
                existente.cantidad = kardex.cantidad;
                existente.stockAnterior = kardex.stockAnterior;
                existente.stockNuevo = kardex.stockNuevo;
                existente.motivo = kardex.motivo;
                existente.fecha = kardex.fecha;
                existente.idUsuario = kardex.idUsuario;

                var response = await kardexRepository
                    .PutKardex(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el movimiento."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Movimiento actualizado correctamente."
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

        [HttpDelete("DeleteKardex/{id}")]
        [RequierePermiso(PermisosSistema.kardexModificar)]
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

                var kardex = await kardexRepository.GetKardexById(id);

                if (kardex == null)
                    return NotFound(new { statusCode = 404, message = "El movimiento que se desea eliminar no existe." });

                var response = await kardexRepository.DeleteKardex(kardex);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el movimiento." });

                return Ok(new { statusCode = 200, message = "Movimiento eliminado correctamente." });
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