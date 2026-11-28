using ElQuateDePatty.Models;
using ElQuateDePatty.Services;

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

        /// <summary>
        /// Cobra la cuenta en una sola transacción: comprobante con base e INC, cuenta cerrada,
        /// pedidos pagados, mesa libre y auditoría. Nunca emite dos comprobantes para la misma cuenta.
        /// </summary>
        /// <param name="totalEsperado">Total que vio quien cobra; si no coincide, no se cobra.</param>
        /// <param name="idUsuario">Usuario autenticado que cobra (del token).</param>
        Task<ResultadoCobro> Cobrar(int idCuenta, int idMetodo, decimal? totalEsperado, int idUsuario);
    }
}