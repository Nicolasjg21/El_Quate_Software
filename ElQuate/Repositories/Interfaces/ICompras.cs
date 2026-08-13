using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IComprasReposiroty
    {
        Task<List<Compras>> GetCompras();

        Task<bool> PostCompras(Compras compras);
    }
}
