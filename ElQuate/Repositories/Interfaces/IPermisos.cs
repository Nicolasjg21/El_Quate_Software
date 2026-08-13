using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IPermisosRepository
    {
        Task<List<Permisos>> GetPermisos();

        Task<bool> PostPermisos(Permisos permisos);
    }
}