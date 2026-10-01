using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Repositories
{
    public class CuentasRepository : ICuentasRepository
    {
        private readonly ElQuateDePattyContext context;

        public CuentasRepository(ElQuateDePattyContext context)
        {
            this.context = context;
        }

        public async Task<List<Cuentas>> GetCuentas()
        {
            var data = await context.cuentas.AsNoTracking().ToListAsync();
            return data;
        }

        public async Task<Cuentas?> GetCuentasById(int id)
        {
            var data = await context.cuentas.FirstOrDefaultAsync(x => x.idCuenta == id);
            return data;
        }

        public async Task<bool> PostCuentas(Cuentas cuentas)
        {
            await context.cuentas.AddAsync(cuentas);
            return await context.BoolAsync();
        }

        public async Task<bool> PutCuentas(Cuentas cuentas)
        {
            context.cuentas.Update(cuentas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCuentas(Cuentas cuentas)
        {
            context.cuentas.Remove(cuentas);
            return await context.BoolAsync();
        }
    }
}