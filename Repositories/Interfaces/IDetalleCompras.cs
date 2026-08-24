using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IDetalleComprasRepository
    {
        Task<List<DetalleCompras>> GetDetalleCompras();

        Task<DetalleCompras> GetDetalleComprasById(int id);

        Task<bool> PostDetalleCompras(DetalleCompras detalleCompras);

        Task<bool> PutDetalleCompras(DetalleCompras detalleCompras);

        Task<bool> DeleteDetalleCompras(DetalleCompras detalleCompras);
    }
}