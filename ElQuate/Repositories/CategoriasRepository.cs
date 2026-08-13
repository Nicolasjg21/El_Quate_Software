using Microsoft.EntityFrameworkCore;
using ElQuateSoftware.Models;
using ElQuateSoftwareSoftware.Context;
using ElQuateSoftwareSoftware.Repositories.Interfaces;

namespace ElQuateSoftware.Repositories
{
    public class CategoriasRepository : ICategoriasRepository
    {
        private readonly ElQuateSoftwareContext context;

        public CategoriasRepository(ElQuateSoftwareContext context)
        {
            this.context = context;
        }

        public async Task<List<Categorias>> GetCategorias()
        {
            var data = await context.Categorias.ToListAsync();
            return data;
        }

        public async Task<Categorias> GetCategoriasById(int id)
        {
            var data = await context.Categorias.FirstOrDefaultAsync(x => x.idCategoria == id);
            return data;
        }

        public async Task<bool> PostCategorias(Categorias categorias)
        {
            await context.Categorias.AddAsync(categorias);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PutCategorias(Categorias categorias)
        {
            context.Categorias.Update(categorias);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategorias(Categorias categorias)
        {
            context.Categorias.Remove(categorias);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
