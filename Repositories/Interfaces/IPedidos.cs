using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IPedidosRepository
    {
        Task<List<Pedidos>> GetPedidos();

        Task<Pedidos> GetPedidosById(int id);

        Task<bool> PostPedidos(Pedidos pedidos);

        Task<bool> PutPedidos(Pedidos pedidos);

        Task<bool> DeletePedidos(Pedidos pedidos);
    }
}