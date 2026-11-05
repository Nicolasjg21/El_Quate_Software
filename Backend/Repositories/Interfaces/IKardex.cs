using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IKardexRepository
    {
        Task<List<Kardex>> GetKardex();

        Task<Kardex?> GetKardexById(int id);

        Task<bool> PostKardex(Kardex kardex);

        ///
        /// Registra un movimiento calculando en el servidor el stock anterior y el nuevo.
        /// Devuelve registrado = false y el stock disponible si una salida dejaría el stock negativo.
        /// 
        Task<(bool registrado, int stock)> RegistrarMovimiento(Kardex kardex);

        Task<bool> PutKardex(Kardex kardex);

        Task<bool> DeleteKardex(Kardex kardex);
    }
}