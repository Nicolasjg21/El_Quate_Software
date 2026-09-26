using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IUsuariosRepository
    {
        Task<List<Usuarios>> GetUsuarios();

        Task<Usuarios?> GetUsuariosById(int id);

        Task<bool> PostUsuarios(Usuarios usuarios);

        Task<bool> PutUsuarios(Usuarios usuarios);

        Task<bool> DeleteUsuarios(Usuarios usuarios);
    }
}