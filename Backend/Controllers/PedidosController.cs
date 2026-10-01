using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidosRepository pedidosRepository;
        private readonly ILogger<PedidosController> logger;

        public PedidosController(
            IPedidosRepository repository,
            ILogger<PedidosController> logger)
        {
            pedidosRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetPedidos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPedidos()
        {
            try
            {
                var pedidos = await pedidosRepository.GetPedidos();

                if (pedidos == null || !pedidos.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron pedidos."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Pedidos obtenidos correctamente.",
                    data = pedidos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los pedidos."
                });
            }
        }

        [HttpGet("GetPedidosById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPedidosById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var pedido = await pedidosRepository.GetPedidosById(id);

                if (pedido == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el pedido."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Pedido obtenido correctamente.",
                    data = pedido.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el pedido."
                });
            }
        }

        [HttpPost("PostPedidos")]
        [RequierePermiso(PermisosSistema.pedidosGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostPedidos([FromBody] PedidoCrearDTO dto)
        {
            try
            {
                var pedido = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                var response = await pedidosRepository.PostPedidos(pedido);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el pedido."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Pedido registrado correctamente.",
                    data = pedido.ToRespuestaDTO()
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
                    message = "Ocurrió un error interno al registrar el pedido."
                });
            }
        }

        [HttpPut("PutPedidos")]
        [RequierePermiso(PermisosSistema.pedidosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutPedidos([FromBody] PedidoActualizarDTO pedido)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                if (pedido.idPedido <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await pedidosRepository.GetPedidosById(pedido.idPedido);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el pedido que desea actualizar."
                    });
                }

                existente.idCuenta = pedido.idCuenta;
                existente.idUsuario = pedido.idUsuario;
                existente.fecha = pedido.fecha;
                existente.estadoPedido = pedido.estadoPedido;

                var response = await pedidosRepository.PutPedidos(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el pedido."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Pedido actualizado correctamente.",
                    data = existente.ToRespuestaDTO()
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

        [HttpDelete("DeletePedidos/{id}")]
        [RequierePermiso(PermisosSistema.pedidosGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeletePedidos(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var pedido = await pedidosRepository.GetPedidosById(id);

                if (pedido == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el pedido."
                    });
                }

                var response = await pedidosRepository.DeletePedidos(pedido);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el pedido."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Pedido eliminado correctamente."
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
                    message = "Ocurrió un error interno al eliminar el pedido."
                });
            }
        }


        [HttpPost("filtrar")]
        public async Task<IActionResult> FiltrarPedidos([FromBody] PedidoFiltroDTO filtro)
        {
            var resultado = await pedidosRepository.FiltrarPedidos(filtro);

            return Ok(resultado);
        }

        [HttpGet("Historial")]
        public async Task<IActionResult> FiltrarHistorialPedidos([FromQuery] PeriodoFiltroDTO filtro)
        {
            var data = await pedidosRepository.FiltrarHistorialPedidos(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Historial de pedidos consultado correctamente",
                data = data.ToRespuestaDTO()
            });
        }
    }
}