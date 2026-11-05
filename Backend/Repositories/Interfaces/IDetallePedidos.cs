using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IDetallePedidosRepository
    {
        /// <summary>
        /// Lista las líneas de pedido. Si se indica <paramref name="idCuenta"/>, devuelve solo las
        /// líneas de los pedidos de esa cuenta; sin él devuelve todas.
        /// </summary>
        Task<List<DetallePedidos>> GetDetallePedidos(int? idCuenta = null);

        Task<DetallePedidos?> GetDetallePedidosById(int id);

        Task<bool> PostDetallePedidos(DetallePedidos detallePedidos);

        Task<bool> PutDetallePedidos(DetallePedidos detallePedidos);

        Task<bool> DeleteDetallePedidos(DetallePedidos detallePedidos);
    }
}