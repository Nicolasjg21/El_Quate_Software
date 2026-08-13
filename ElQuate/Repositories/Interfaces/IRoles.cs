using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IRolesRepository
    {
        Task<List<Roles>> GetRoles();

        Task<bool> PostRoles(Roles roles);
    }
}