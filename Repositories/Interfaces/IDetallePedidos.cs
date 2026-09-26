using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IDetallePedidosRepository
    {
        Task<List<DetallePedidos>> GetDetallePedidos();

        Task<DetallePedidos?> GetDetallePedidosById(int id);

        Task<bool> PostDetallePedidos(DetallePedidos detallePedidos);

        Task<bool> PutDetallePedidos(DetallePedidos detallePedidos);

        Task<bool> DeleteDetallePedidos(DetallePedidos detallePedidos);
    }
}