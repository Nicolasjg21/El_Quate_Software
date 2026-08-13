using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IMetodosPagoRepository
    {
        Task<List<MetodosPago>> GetMetodosPago();

        Task<bool> PostMetodosPago(MetodosPago metodosPago);

    }
}