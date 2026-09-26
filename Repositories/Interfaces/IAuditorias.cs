using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IAuditoriasRepository
    {
        Task<List<Auditorias>> GetAuditorias();

        Task<Auditorias?> GetAuditoriasById(int id);

        Task<bool> PostAuditorias(Auditorias auditorias);

        Task<bool> PutAuditorias(Auditorias auditorias);

        Task<bool> DeleteAuditorias(Auditorias auditorias);
    }
}