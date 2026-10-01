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

    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriasRepository _categoriasRepository;

        public CategoriasController(ICategoriasRepository categoriasRepository)
        {
            _categoriasRepository = categoriasRepository;
        }

        [HttpGet("GetCategorias")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategorias()
        {
            try
            {
                var response = await _categoriasRepository.GetCategorias();

                if (response == null || response.Count == 0)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "No se encontró información de categorías."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Consulta de categorías realizada correctamente.",
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    statusCode = StatusCodes.Status400BadRequest,
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    statusCode = StatusCodes.Status401Unauthorized,
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    statusCode = StatusCodes.Status404NotFound,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpGet("GetCategoriaById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategoriaById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = StatusCodes.Status400BadRequest,
                        message = "El identificador debe ser mayor que cero."
                    });

                var response = await _categoriasRepository.GetCategoriasById(id);

                if (response == null)
                    return NotFound(new
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        message = "La categoría solicitada no existe."
                    });

                return Ok(new
                {
                    statusCode = StatusCodes.Status200OK,
                    message = "Categoría encontrada correctamente.",
                    data = response
                });
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
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPost("PostCategoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCategoria([FromBody] Categorias categorias)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (categorias == null)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la categoría son obligatorios."
                    });

                var response = await _categoriasRepository.PostCategorias(categorias);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar la categoría."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Categoría registrada correctamente."
                });
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
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }

        [HttpPut("PutCategoria")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCategoria([FromBody] Categorias categorias)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (categorias == null || categorias.idCategoria <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la categoría no son válidos."
                    });

                var existente = await _categoriasRepository.GetCategoriasById(categorias.idCategoria);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La categoría que se desea actualizar no existe."
                    });

                existente.nombreCategoria = categorias.nombreCategoria;

                var response = await _categoriasRepository.PutCategorias(existente);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible actualizar la categoría."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Categoría actualizada correctamente."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
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

        [HttpDelete("DeleteCategoria/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "El identificador debe ser mayor que cero."
                    });

                var categoria = await _categoriasRepository.GetCategoriasById(id);

                if (categoria == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La categoría que se desea eliminar no existe."
                    });

                var response = await _categoriasRepository.DeleteCategorias(categoria);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible eliminar la categoría."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Categoría eliminada correctamente."
                });
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
                return StatusCode(500, new
                {
                    statusCode = 500,
                    message = "Ocurrió un error interno en el servidor."
                });
            }
        }
    }
}