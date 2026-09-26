using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IComprobantesRepository
    {
        Task<List<Comprobantes>> GetComprobantes();

        Task<Comprobantes?> GetComprobantesById(int id);

        Task<bool> PostComprobantes(Comprobantes comprobantes);

        Task<bool> PutComprobantes(Comprobantes comprobantes);

        Task<bool> DeleteComprobantes(Comprobantes comprobantes);
    }
}