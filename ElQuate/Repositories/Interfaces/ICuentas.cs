using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface ICuentasRepository
    {
        Task<List<Cuentas>> GetCuentas();

        Task<bool> PostCuentas(Cuentas cuentas);
    }
}
