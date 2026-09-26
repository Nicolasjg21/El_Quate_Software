using ElQuateDePatty.DTOs;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IComprasRepository
    {
        Task<List<Compras>> GetCompras();

        Task<Compras?> GetComprasById(int id);

        Task<bool> PostCompras(Compras compras);

        Task<bool> PutCompras(Compras compras);

        Task<bool> DeleteCompras(Compras compras);

        Task<List<Compras>> FiltrarCompras(CompraFiltroDTO filtro);

        Task<List<Compras>> FiltrarHistorialCompras(PeriodoFiltroDTO filtro);
    }
}