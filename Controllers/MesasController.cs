using ElQuateDePatty.DTOs;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    [Authorize]

    public class MesasController : ControllerBase
    {
        private readonly IMesasRepository _mesasRepository;

        public MesasController(IMesasRepository mesasRepository)
        {
            _mesasRepository = mesasRepository;
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
                var response = await _mesasRepository.GetMesas();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de mesas." });

                return Ok(new { statusCode = 200, message = "Consulta de mesas realizada correctamente.", data = response });
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

                var response = await _mesasRepository.GetMesasById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "La mesa solicitada no existe." });

                return Ok(new { statusCode = 200, message = "Mesa encontrada correctamente.", data = response });
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

                var response = await _mesasRepository.GetMesasByEstado(estado);

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
                    data = response
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = 404,
                    message = ex.Message
                });
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

        [HttpPut("PutMesa")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutMesa([FromBody] Mesas mesas)
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

                var existente = await _mesasRepository
                    .GetMesasById(mesas.idMesa);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La mesa que se desea actualizar no existe."
                    });
                }

                // Aquí copiamos los campos de Mesas.
                // Necesito tu modelo Mesas para ponerlos exactamente.

                var response = await _mesasRepository
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpDelete("DeleteMesa/{id}")]
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

                var mesa = await _mesasRepository.GetMesasById(id);

                if (mesa == null)
                    return NotFound(new { statusCode = 404, message = "La mesa que se desea eliminar no existe." });

                var response = await _mesasRepository.DeleteMesas(mesa);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar la mesa." });

                return Ok(new { statusCode = 200, message = "Mesa eliminada correctamente." });
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

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarMesas([FromQuery] MesaFiltroDTO filtro)
        {
            var data = await _mesasRepository.FiltrarMesas(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Mesas filtradas correctamente",
                data = data
            });
        }
    }
}