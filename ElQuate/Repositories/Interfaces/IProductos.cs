using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IProductosRepository
    {
        Task<List<Productos>> GetProductos();

        Task<bool> PostProductos(Productos productos);
    }
}