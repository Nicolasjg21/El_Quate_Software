using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class ProveedoresController : ControllerBase
    {
        private readonly IProveedoresRepository _proveedoresRepository;

        public ProveedoresController(IProveedoresRepository proveedoresRepository)
        {
            _proveedoresRepository = proveedoresRepository;
        }

        [HttpGet("GetProveedores")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProveedores()
        {
            try
            {
                var response = await _proveedoresRepository.GetProveedores();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de proveedores." });

                return Ok(new { statusCode = 200, message = "Consulta de proveedores realizada correctamente.", data = response });
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

        [HttpGet("GetProveedorById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProveedorById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _proveedoresRepository.GetProveedoresById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El proveedor solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Proveedor encontrado correctamente.", data = response });
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

        [HttpPost("PostProveedor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostProveedor([FromBody] Proveedores proveedores)
        {
            try
            {
                if (!ModelState.IsValid || proveedores == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del proveedor no son válidos." });

                var response = await _proveedoresRepository.PostProveedores(proveedores);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el proveedor." });

                return Ok(new { statusCode = 200, message = "Proveedor registrado correctamente." });
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

        [HttpPut("PutProveedor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutProveedor([FromBody] Proveedores proveedores)
        {
            try
            {
                if (!ModelState.IsValid || proveedores == null || proveedores.idProveedor <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos del proveedor no son válidos."
                    });

                var existente = await _proveedoresRepository.GetProveedoresById(proveedores.idProveedor);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "El proveedor que se desea actualizar no existe."
                    });

                existente.nombreProveedor = proveedores.nombreProveedor;
                existente.telefono = proveedores.telefono;
                existente.direccion = proveedores.direccion;

                var response = await _proveedoresRepository.PutProveedores(existente);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar el proveedor."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Proveedor actualizado correctamente."
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

        [HttpDelete("DeleteProveedor/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteProveedor(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var proveedor = await _proveedoresRepository.GetProveedoresById(id);

                if (proveedor == null)
                    return NotFound(new { statusCode = 404, message = "El proveedor que se desea eliminar no existe." });

                var response = await _proveedoresRepository.DeleteProveedores(proveedor);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el proveedor." });

                return Ok(new { statusCode = 200, message = "Proveedor eliminado correctamente." });
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