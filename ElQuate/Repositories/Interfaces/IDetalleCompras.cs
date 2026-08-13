using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IDetallecomprasRepository
    {
        Task<List<DetalleCompras>> GetDetalleCompras();

        Task<bool> PostDetalleCompras(DetalleCompras detalle);
    }
}
