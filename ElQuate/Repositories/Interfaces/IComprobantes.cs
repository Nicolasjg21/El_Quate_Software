using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IComprobantesRepository
    {
        Task<List<Comprobantes>> GetComprobantes();

        Task<bool> PostComprobantes(Comprobantes comprobantes);
    }
}
