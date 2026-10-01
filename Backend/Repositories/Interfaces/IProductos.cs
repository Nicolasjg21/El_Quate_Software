using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IProductosRepository
    {
        Task<List<Productos>> GetProductos();

        Task<Productos?> GetProductosById(int id);

        Task<bool> PostProductos(Productos productos);

        Task<bool> PutProductos(Productos productos);

        Task<bool> DeleteProductos(Productos productos);

        Task<List<ProductoRespuestaDTO>> FiltrarProductos(ProductoFiltroDTO filtro);
    }
}