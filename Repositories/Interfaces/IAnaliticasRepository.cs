using ElQuateDePatty.DTOs.Analiticas;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IAnaliticasRepository
    {
        Task<VentaAnaliticaDTO> ObtenerAnaliticasVentas(PeriodoFiltroDTO filtro);
    }
}