using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IMesasRepository
    {
        Task<List<Mesas>> GetMesas();

        Task<bool> PostMesas(Mesas mesas);
    }
}