using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Context;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class TipoDocumentoRepository : ITipoDocumentoRepository
    {
        private readonly ElQuateDePattyContext context;

        public TipoDocumentoRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<TipoDocumento>> GetTipoDocumento()
        {
            var data = await context.tipoDocumento.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<TipoDocumento?> GetTipoDocumentoById(int id)
        {
            var data = await context.tipoDocumento.FirstOrDefaultAsync(x => x.idTipoDocumento == id);
            return data;
        }

        public async Task<bool> PostTipoDocumento(TipoDocumento tipoDocumento)
        {
            await context.tipoDocumento.AddAsync(tipoDocumento);
            return await context.BoolAsync();
        }

        public async Task<bool> PutTipoDocumento(TipoDocumento tipoDocumento)
        {
            context.tipoDocumento.Update(tipoDocumento);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteTipoDocumento(TipoDocumento tipoDocumento)
        {
            context.tipoDocumento.Remove(tipoDocumento);
            return await context.BoolAsync();
        }
    }
}