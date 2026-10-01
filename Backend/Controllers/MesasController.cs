using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class MesasController : ControllerBase
    {
        private readonly IMesasRepository mesasRepository;
        private readonly ILogger<MesasController> logger;

        public MesasController(
            IMesasRepository mesasRepository,
            ILogger<MesasController> logger)
        {
            this.mesasRepository = mesasRepository;
            this.logger = logger;
        }

        [HttpGet("GetMesas")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMesas()
        {
            try
            {
                var response = await mesasRepository.GetMesas();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de mesas." });

                return Ok(new { statusCode = 200, message = "Consulta de mesas realizada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("GetMesaById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMesaById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await mesasRepository.GetMesasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La mesa solicitada no existe." });

                return Ok(new { statusCode = 200, message = "Mesa encontrada correctamente.", data = response.ToRespuestaDTO() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new { statusCode = 500, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet("GetMesasByEstado/{estado}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMesasByEstado(string estado)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(estado))
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El estado de la mesa es requerido."
                    });
                }

                var response = await mesasRepository.GetMesasByEstado(estado);

                if (response == null || response.Count == 0)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron mesas con el estado indicado."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Consulta de mesas por estado realizada correctamente.",
                    data = response.ToRespuestaDTO()
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

        [HttpPut("PutMesa")]
        [RequierePermiso(PermisosSistema.mesasGestionar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutMesa([FromBody] MesaActualizarDTO mesas)
        {
            try
            {
                if (!ModelState.IsValid || mesas == null || mesas.idMesa <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la mesa no son válidos."
                    });
                }

                var existente = await mesasRepository
                    .GetMesasById(mesas.idMesa);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La mesa que se desea actualizar no existe."
                    });
                }

                existente.numeroMesa = mesas.numeroMesa;
                existente.estado = mesas.estado;

                var response = await mesasRepository
                    .PutMesas(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la mesa."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Mesa actualizada correctamente."
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

        [HttpDelete("DeleteMesa/{id}")]
        [RequierePermiso(PermisosSistema.mesasGestionar)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteMesa(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var mesa = await mesasRepository.GetMesasById(id);

                if (mesa == null)
                    return NotFound(new { statusCode = 404, message = "La mesa que se desea eliminar no existe." });

                var response = await mesasRepository.DeleteMesas(mesa);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la mesa." });

                return Ok(new { statusCode = 200, message = "Mesa eliminada correctamente." });
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

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarMesas([FromQuery] MesaFiltroDTO filtro)
        {
            var data = await mesasRepository.FiltrarMesas(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Mesas filtradas correctamente",
                data = data.ToRespuestaDTO()
            });
        }
    }
}