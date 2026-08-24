using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IKardexRepository
    {
        Task<List<Kardex>> GetKardex();

        Task<Kardex> GetKardexById(int id);

        Task<bool> PostKardex(Kardex kardex);

        Task<bool> PutKardex(Kardex kardex);

        Task<bool> DeleteKardex(Kardex kardex);
    }
}