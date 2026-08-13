using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface IProveedoresRepository
    {
        Task<List<Proveedores>> GetProveedores();

        Task<bool> PostProveedores(Proveedores proveedores);
    }
}