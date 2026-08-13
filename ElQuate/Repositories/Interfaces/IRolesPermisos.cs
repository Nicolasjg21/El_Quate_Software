using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IRolesPermisosRepository
    {
        Task<List<RolesPermisos>> GetRolesPermisos();

        Task<bool> PostRolesPermisos(RolesPermisos rolesPermisos);
    }
}