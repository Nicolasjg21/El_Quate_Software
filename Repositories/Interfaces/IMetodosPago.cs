using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IMetodosPagoRepository
    {
        Task<List<MetodosPago>> GetMetodosPago();

        Task<MetodosPago> GetMetodosPagoById(int id);

        Task<bool> PostMetodosPago(MetodosPago metodosPago);

        Task<bool> PutMetodosPago(MetodosPago metodosPago);

        Task<bool> DeleteMetodosPago(MetodosPago metodosPago);
    }
}