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
            var data = await context.Cuentas.ToListAsync();
            return data;
        }

        public async Task<Cuentas> GetCuentasById(int id)
        {
            var data = await context.Cuentas.FirstOrDefaultAsync(x => x.idCuenta == id);
            return data;
        }

        public async Task<bool> PostCuentas(Cuentas cuentas)
        {
            await context.Cuentas.AddAsync(cuentas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> PutCuentas(Cuentas cuentas)
        {
            context.Cuentas.Update(cuentas);
            await context.BoolAsync();
            return true;
        }

        public async Task<bool> DeleteCuentas(Cuentas cuentas)
        {
            context.Cuentas.Remove(cuentas);
            await context.BoolAsync();
            return true;
        }
    }
}