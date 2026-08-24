using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IRolesPermisosRepository
    {
        Task<List<RolesPermisos>> GetRolesPermisos();

        Task<RolesPermisos> GetRolesPermisosById(int idRol, int idPermiso);

        Task<bool> PostRolesPermisos(RolesPermisos rolesPermisos);

        Task<bool> PutRolesPermisos(RolesPermisos rolesPermisos);

        Task<bool> DeleteRolesPermisos(RolesPermisos rolesPermisos);
    }
}