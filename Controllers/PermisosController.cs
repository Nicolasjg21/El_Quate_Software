using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequireHttps]
    public class PermisosController : ControllerBase
    {
        private readonly IPermisosRepository _permisosRepository;

        public PermisosController(IPermisosRepository permisosRepository)
        {
            _permisosRepository = permisosRepository;
        }

        [HttpGet("GetPermisos")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPermisos()
        {
            try
            {
                var response = await _permisosRepository.GetPermisos();

                if (response == null || response.Count == 0)
                    return NotFound(new { statusCode = 404, message = "No se encontró información de permisos." });

                return Ok(new { statusCode = 200, message = "Consulta de permisos realizada correctamente.", data = response });
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

        [HttpGet("GetPermisoById/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPermisoById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var response = await _permisosRepository.GetPermisosById(id);

                if (response == null)
                    return NotFound(new { statusCode = 404, message = "El permiso solicitado no existe." });

                return Ok(new { statusCode = 200, message = "Permiso encontrado correctamente.", data = response });
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

        [HttpPost("PostPermiso")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PostPermiso([FromBody] Permisos permisos)
        {
            try
            {
                if (!ModelState.IsValid || permisos == null)
                    return BadRequest(new { statusCode = 400, message = "Los datos del permiso no son válidos." });

                var response = await _permisosRepository.PostPermisos(permisos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible registrar el permiso." });

                return Ok(new { statusCode = 200, message = "Permiso registrado correctamente." });
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

        [HttpPut("PutPermiso")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PutPermiso([FromBody] Permisos permisos)
        {
            try
            {
                if (!ModelState.IsValid || permisos == null || permisos.idPermiso <= 0)
                    return BadRequest(new { statusCode = 400, message = "Los datos del permiso no son válidos." });

                var existente = await _permisosRepository.GetPermisosById(permisos.idPermiso);

                if (existente == null)
                    return NotFound(new { statusCode = 404, message = "El permiso que se desea actualizar no existe." });

                var response = await _permisosRepository.PutPermisos(permisos);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible actualizar el permiso." });

                return Ok(new { statusCode = 200, message = "Permiso actualizado correctamente." });
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

        [HttpDelete("DeletePermiso/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeletePermiso(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { statusCode = 400, message = "El identificador debe ser mayor que cero." });

                var permiso = await _permisosRepository.GetPermisosById(id);

                if (permiso == null)
                    return NotFound(new { statusCode = 404, message = "El permiso que se desea eliminar no existe." });

                var response = await _permisosRepository.DeletePermisos(permiso);

                if (!response)
                    return BadRequest(new { statusCode = 400, message = "No fue posible eliminar el permiso." });

                return Ok(new { statusCode = 200, message = "Permiso eliminado correctamente." });
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