using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IPermisosRepository
    {
        Task<List<Permisos>> GetPermisos();

        Task<Permisos> GetPermisosById(int id);

        Task<bool> PostPermisos(Permisos permisos);

        Task<bool> PutPermisos(Permisos permisos);

        Task<bool> DeletePermisos(Permisos permisos);
    }
}