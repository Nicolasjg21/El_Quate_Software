using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IDetallePedidosRepository
    {
        Task<List<DetallePedidos>> GetDetallePedidos();

        Task<bool> PostDetallePedidos(DetallePedidos detallePedidos);
    }

}
