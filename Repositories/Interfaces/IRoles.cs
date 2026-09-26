using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IRolesRepository
    {
        Task<List<Roles>> GetRoles();

        Task<Roles?> GetRolesById(int id);

        Task<bool> PostRoles(Roles roles);

        Task<bool> PutRoles(Roles roles);

        Task<bool> DeleteRoles(Roles roles);
    }
}