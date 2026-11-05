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

        /// <summary>
        /// Lista las cuentas. El filtro por estado se resuelve en la base de datos, de modo que
        /// solo viajan las filas solicitadas (por ejemplo, las cuentas abiertas que usa Mesas).
        /// </summary>
        public async Task<List<Cuentas>> GetCuentas(string? estado = null)
        {
            var consulta = context.cuentas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var filtro = estado.Trim();
                consulta = consulta.Where(c => c.estado == filtro);
            }

            return await consulta.ToListAsync();
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