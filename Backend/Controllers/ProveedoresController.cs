using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProveedoresController : ControllerBase
    {
        private readonly IProveedoresRepository proveedoresRepository;
        private readonly ILogger<ProveedoresController> logger;

        public ProveedoresController(
            IProveedoresRepository repository,
            ILogger<ProveedoresController> logger)
        {
            proveedoresRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetProveedores")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProveedores()
        {
            try
            {
                var proveedores = await proveedoresRepository.GetProveedores();

                if (proveedores == null || !proveedores.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron proveedores."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Proveedores obtenidos correctamente.",
                    data = proveedores.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los proveedores."
                });
            }
        }

        [HttpGet("GetProveedoresById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProveedoresById(int id)
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

                var proveedor = await proveedoresRepository.GetProveedoresById(id);

                if (proveedor == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el proveedor."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Proveedor obtenido correctamente.",
                    data = proveedor.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el proveedor."
                });
            }
        }

        [HttpPost("PostProveedores")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostProveedores([FromBody] ProveedorCrearDTO dto)
        {
            try
            {
                var proveedor = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                var response = await proveedoresRepository.PostProveedores(proveedor);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el proveedor."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Proveedor registrado correctamente.",
                    data = proveedor.ToRespuestaDTO()
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
                    message = "Ocurrió un error interno al registrar el proveedor."
                });
            }
        }

        [HttpPut("PutProveedores")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutProveedores([FromBody] ProveedorActualizarDTO proveedor)
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

                if (proveedor.idProveedor <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await proveedoresRepository.GetProveedoresById(proveedor.idProveedor);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el proveedor que desea actualizar."
                    });
                }

                existente.nombreProveedor = proveedor.nombreProveedor;
                existente.telefono = proveedor.telefono;
                existente.direccion = proveedor.direccion;

                var response = await proveedoresRepository.PutProveedores(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el proveedor."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Proveedor actualizado correctamente.",
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

        [HttpDelete("DeleteProveedores/{id}")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProveedores(int id)
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

                var proveedor = await proveedoresRepository.GetProveedoresById(id);

                if (proveedor == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el proveedor."
                    });
                }

                var response = await proveedoresRepository.DeleteProveedores(proveedor);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el proveedor."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Proveedor eliminado correctamente."
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
                    message = "Ocurrió un error interno al eliminar el proveedor."
                });
            }
        }

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarProveedores([FromQuery] ProveedorFiltroDTO filtro)
        {
            var data = await proveedoresRepository.FiltrarProveedores(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Proveedores filtrados correctamente",
                data = data.ToRespuestaDTO()
            });
        }
    }
}