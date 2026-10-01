using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class CategoriasRepository : ICategoriasRepository
    {
        private readonly ElQuateDePattyContext context;

        public CategoriasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Categorias>> GetCategorias()
        {
            var data = await context.categorias.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Categorias?> GetCategoriasById(int id)
        {
            var data = await context.categorias.FirstOrDefaultAsync(x => x.idCategoria == id);
            return data;
        }

        public async Task<bool> PostCategorias(Categorias categorias)
        {
            await context.categorias.AddAsync(categorias);
            return await context.BoolAsync();
        }

        public async Task<bool> PutCategorias(Categorias categorias)
        {
            context.categorias.Update(categorias);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCategorias(Categorias categorias)
        {
            context.categorias.Remove(categorias);
            return await context.BoolAsync();
        }
    }
}