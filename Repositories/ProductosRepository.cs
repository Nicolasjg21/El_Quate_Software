using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ProductosRepository : IProductosRepository
    {
        private readonly ElQuateDePattyContext context;

        public ProductosRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Productos>> GetProductos()
        {
            var data = await context.Productos.ToListAsync();
            return data;
        }

        public async Task<Productos> GetProductosById(int id)
        {
            var data = await context.Productos.FirstOrDefaultAsync(x => x.idProducto == id);
            return data;
        }

        public async Task<bool> PostProductos(Productos productos)
        {
            await context.Productos.AddAsync(productos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutProductos(Productos productos)
        {
            context.Productos.Update(productos);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteProductos(Productos productos)
        {
            context.Productos.Remove(productos);
            await context.BoolAsync();
            return true;
        }
    }
}