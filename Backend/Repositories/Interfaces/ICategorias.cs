using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface ICategoriasRepository
    {
        Task<List<Categorias>> GetCategorias();

        Task<Categorias?> GetCategoriasById(int id);

        Task<bool> PostCategorias(Categorias categorias);

        Task<bool> PutCategorias(Categorias categorias);

        Task<bool> DeleteCategorias(Categorias categorias);
    }
}