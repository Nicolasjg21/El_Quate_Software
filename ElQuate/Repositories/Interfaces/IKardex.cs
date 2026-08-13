using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IKardexRepository
    {
        Task<List<Kardex>> GetKardex();

        Task<bool> PostKardex(Kardex kardex);
    }
}
