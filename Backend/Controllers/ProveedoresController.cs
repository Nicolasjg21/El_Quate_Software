using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
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
        private readonly IProveedoresRepository _proveedoresRepository;

        public ProveedoresController(IProveedoresRepository repository)
        {
            _proveedoresRepository = repository;
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
                var proveedores = await _proveedoresRepository.GetProveedores();

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
                    data = proveedores
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

                var proveedor = await _proveedoresRepository.GetProveedoresById(id);

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
                    data = proveedor
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
                    message = "Ocurrió un error interno al obtener el proveedor."
                });
            }
        }

        [HttpPost("PostProveedores")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostProveedores([FromBody] Proveedores proveedor)
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

                var response = await _proveedoresRepository.PostProveedores(proveedor);

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
                    data = proveedor
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
                    message = "Ocurrió un error interno al registrar el proveedor."
                });
            }
        }

        [HttpPut("PutProveedores")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutProveedores([FromBody] Proveedores proveedor)
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

                var existente = await _proveedoresRepository.GetProveedoresById(proveedor.idProveedor);

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

                var response = await _proveedoresRepository.PutProveedores(existente);

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
                    data = existente
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

        [HttpDelete("DeleteProveedores/{id}")]
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

                var proveedor = await _proveedoresRepository.GetProveedoresById(id);

                if (proveedor == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el proveedor."
                    });
                }

                var response = await _proveedoresRepository.DeleteProveedores(proveedor);

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
                    message = "Ocurrió un error interno al eliminar el proveedor."
                });
            }
        }

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarProveedores([FromQuery] ProveedorFiltroDTO filtro)
        {
            var data = await _proveedoresRepository.FiltrarProveedores(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Proveedores filtrados correctamente",
                data = data
            });
        }
    }
}