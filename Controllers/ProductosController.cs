using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly IProductosRepository _productosRepository;

        public ProductosController(IProductosRepository productosRepository)
        {
            _productosRepository = productosRepository;
        }

        [HttpGet("GetProductos")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductos()
        {
            try
            {
                var response = await _productosRepository.GetProductos();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de productos." });

                return Ok(new { statusCode = 200, message = "Consulta de productos realizada correctamente.", data = response });
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

        [HttpGet("GetProductoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProductoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _productosRepository.GetProductosById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El producto solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Producto encontrado correctamente.", data = response });
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

        [HttpPost("PostProducto")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostProducto([FromBody] Productos productos)
        {
            try
            {
                if (!ModelState.IsValid || productos == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del producto no son válidos." });

                var response = await _productosRepository.PostProductos(productos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el producto." });

                return Ok(new { statusCode = 200, message = "Producto registrado correctamente." });
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

        [HttpPut("PutProducto")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutProducto([FromBody] Productos productos)
        {
            try
            {
                if (!ModelState.IsValid || productos == null || productos.idProducto <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del producto no son válidos." });

                var existente = await _productosRepository.GetProductosById(productos.idProducto);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El producto que se desea actualizar no existe." });

                var response = await _productosRepository.PutProductos(productos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el producto." });

                return Ok(new { statusCode = 200, message = "Producto actualizado correctamente." });
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

        [HttpDelete("DeleteProducto/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var producto = await _productosRepository.GetProductosById(id);

                if (producto == null)
                    return NotFound(new { statusCode = 404, message = "El producto que se desea eliminar no existe." });

                var response = await _productosRepository.DeleteProductos(producto);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el producto." });

                return Ok(new { statusCode = 200, message = "Producto eliminado correctamente." });
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