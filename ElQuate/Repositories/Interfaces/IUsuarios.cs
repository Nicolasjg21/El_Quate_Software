using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IUsuariosRepository
    {
        Task<List<Usuarios>> GetUsuarios();

        Task<bool> PostUsuarios(Usuarios usuarios);
    }
}