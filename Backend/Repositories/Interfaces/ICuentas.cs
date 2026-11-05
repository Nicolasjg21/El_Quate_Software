using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface ICuentasRepository
    {
        /// <summary>
        /// Lista las cuentas. Si se indica <paramref name="estado"/> (por ejemplo, Abierta),
        /// devuelve solo las de ese estado; sin él devuelve todas.
        /// </summary>
        Task<List<Cuentas>> GetCuentas(string? estado = null);

        Task<Cuentas?> GetCuentasById(int id);

        Task<bool> PostCuentas(Cuentas cuentas);

        Task<bool> PutCuentas(Cuentas cuentas);

        Task<bool> DeleteCuentas(Cuentas cuentas);
    }
}