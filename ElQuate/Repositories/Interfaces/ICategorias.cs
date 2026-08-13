using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface ICategoriasRepository
    {
        Task<List<Categorias>> GetCategorias();

        Task<bool> PostCategorias(Categorias categorias);
    }
}
