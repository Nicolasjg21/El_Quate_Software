using ElQuateDePatty.DTOs.Analiticas;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElQuateDePatty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnaliticasController : ControllerBase
    {
        private readonly IAnaliticasRepository _analiticasRepository;

        public AnaliticasController(
            IAnaliticasRepository repository)
        {
            _analiticasRepository = repository;
        }

        [HttpGet("Ventas")]
        public async Task<IActionResult> ObtenerAnaliticasVentas(
            [FromQuery] PeriodoFiltroDTO filtro)
        {
            var data =
                await _analiticasRepository.ObtenerAnaliticasVentas(filtro);

            return Ok(new
            {
                statusCode = 200,
                message = "Analíticas de ventas obtenidas correctamente",
                data = data
            });
        }
    }
}