using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IPedidosRepository
    {
        Task<List<Pedidos>> GetPedidos();

        Task<bool> PostPedidos(Pedidos pedidos);
    }
}