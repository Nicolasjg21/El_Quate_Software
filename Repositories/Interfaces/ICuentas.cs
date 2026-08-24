using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface ICuentasRepository
    {
        Task<List<Cuentas>> GetCuentas();

        Task<Cuentas> GetCuentasById(int id);

        Task<bool> PostCuentas(Cuentas cuentas);

        Task<bool> PutCuentas(Cuentas cuentas);

        Task<bool> DeleteCuentas(Cuentas cuentas);
    }
}