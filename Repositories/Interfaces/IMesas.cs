using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IMesasRepository
    {
        Task<List<Mesas>> GetMesas();

        Task<Mesas> GetMesasById(int id);

        Task<List<Mesas>> GetMesasByEstado(string estado);

        Task<bool> PostMesas(Mesas mesas);

        Task<bool> PutMesas(Mesas mesas);

        Task<bool> DeleteMesas(Mesas mesas);
    }
}