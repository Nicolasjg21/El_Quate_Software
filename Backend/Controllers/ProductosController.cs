using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly IProductosRepository productosRepository;
        private readonly ILogger<ProductosController> logger;

        public ProductosController(
            IProductosRepository repository,
            ILogger<ProductosController> logger)
        {
            productosRepository = repository;
            this.logger = logger;
        }

        [HttpGet("GetProductos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductos()
        {
            try
            {
                var productos = await productosRepository.GetProductos();

                if (productos == null || !productos.Any())
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontraron productos."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Productos obtenidos correctamente.",
                    data = productos.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener los productos."
                });
            }
        }

        [HttpGet("GetProductosById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductosById(int id)
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

                var producto = await productosRepository.GetProductosById(id);

                if (producto == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el producto."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Producto obtenido correctamente.",
                    data = producto.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno al obtener el producto."
                });
            }
        }

        [HttpPost("PostProductos")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostProductos([FromBody] ProductoCrearDTO dto)
        {
            try
            {
                var producto = dto.ToEntity();

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos enviados no son válidos.",
                        errors = ModelState
                    });
                }

                var response = await productosRepository.PostProductos(producto);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar el producto."
                    });
                }

                return StatusCode(201, new
                {
                    statusCode = 201,
                    message = "Producto registrado correctamente.",
                    data = producto.ToRespuestaDTO()
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
                    message = "Ocurrió un error interno al registrar el producto."
                });
            }
        }

        [HttpPut("PutProductos")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutProductos([FromBody] ProductoActualizarDTO producto)
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

                if (producto.idProducto <= 0)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El ID debe ser mayor que cero."
                    });
                }

                var existente = await productosRepository.GetProductosById(producto.idProducto);

                if (existente == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el producto que desea actualizar."
                    });
                }

                existente.nombreProducto = producto.nombreProducto;
                existente.precioVenta = producto.precioVenta;
                existente.cantidadMinima = producto.cantidadMinima;
                existente.estado = producto.estado;
                existente.idCategoria = producto.idCategoria;

                var response = await productosRepository.PutProductos(existente);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el producto."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Producto actualizado correctamente.",
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

        [HttpDelete("DeleteProductos/{id}")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProductos(int id)
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

                var producto = await productosRepository.GetProductosById(id);

                if (producto == null)
                {
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "No se encontró el producto."
                    });
                }

                var response = await productosRepository.DeleteProductos(producto);

                if (!response)
                {
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar el producto."
                    });
                }

                return Ok(new
                {
                    statusCode = 200,
                    message = "Producto eliminado correctamente."
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
                    message = "Ocurrió un error interno al eliminar el producto."
                });
            }

        }

        [HttpGet("Filtrar")]
        public async Task<IActionResult> FiltrarProductos([FromQuery] ProductoFiltroDTO filtro)
        {
            var data = await productosRepository.FiltrarProductos(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Productos filtrados correctamente",
                data = data
            });
        }
    }
}