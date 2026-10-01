using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class ComprobantesRepository : IComprobantesRepository
    {
        private readonly ElQuateDePattyContext context;

        public ComprobantesRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Comprobantes>> GetComprobantes()
        {
            var data = await context.Comprobantes.ToListAsync();
            return data;
        }

        public async Task<Comprobantes?> GetComprobantesById(int id)
        {
            var data = await context.Comprobantes.FirstOrDefaultAsync(x => x.idComprobante == id);
            return data;
        }

        public async Task<bool> PostComprobantes(Comprobantes comprobantes)
        {
            await context.Comprobantes.AddAsync(comprobantes);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutComprobantes(Comprobantes comprobantes)
        {
            context.Comprobantes.Update(comprobantes);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteComprobantes(Comprobantes comprobantes)
        {
            context.Comprobantes.Remove(comprobantes);
            await context.BoolAsync();
            return true;
        }
    }
}