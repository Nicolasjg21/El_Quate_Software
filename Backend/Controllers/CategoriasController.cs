using ElQuateDePatty.Services;
using Microsoft.EntityFrameworkCore;
using ElQuateDePatty.DTOs.Categorias;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class CategoriasController : ControllerBase
    {
        private readonly ICategoriasRepository categoriasRepository;
        private readonly ILogger<CategoriasController> logger;

        public CategoriasController(
            ICategoriasRepository categoriasRepository,
            ILogger<CategoriasController> logger)
        {
            this.categoriasRepository = categoriasRepository;
            this.logger = logger;
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
                var response = await categoriasRepository.GetCategorias();

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
                    data = response.ToRespuestaDTO()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", Request.Method, Request.Path);

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

                var response = await categoriasRepository.GetCategoriasById(id);

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

        [HttpPost("PostCategoria")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostCategoria([FromBody] CategoriaCrearDTO dto)
        {
            try
            {
                var categorias = dto.ToEntity();

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (categorias == null)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "Los datos de la categoría son obligatorios."
                    });

                var response = await categoriasRepository.PostCategorias(categorias);

                if (!response)
                    return BadRequest(new
                    {
                        statusCode = 400,
                        message = "No fue posible registrar la categoría."
                    });

                return Ok(new
                {
                    statusCode = 200,
                    message = "Categoría registrada correctamente.",
                    data = categorias.ToRespuestaDTO()
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

        [HttpPut("PutCategoria")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCategoria([FromBody] CategoriaActualizarDTO categorias)
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

                var existente = await categoriasRepository.GetCategoriasById(categorias.idCategoria);

                if (existente == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La categoría que se desea actualizar no existe."
                    });

                existente.nombreCategoria = categorias.nombreCategoria;

                var response = await categoriasRepository.PutCategorias(existente);

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

        [HttpDelete("DeleteCategoria/{id}")]
        [RequierePermiso(PermisosSistema.inventarioGestionar)]
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

                var categoria = await categoriasRepository.GetCategoriasById(id);

                if (categoria == null)
                    return NotFound(new
                    {
                        statusCode = 404,
                        message = "La categoría que se desea eliminar no existe."
                    });

                var response = await categoriasRepository.DeleteCategorias(categoria);

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
    }
}