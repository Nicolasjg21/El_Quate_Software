using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IProveedoresRepository
    {
        Task<List<Proveedores>> GetProveedores();

        Task<Proveedores?> GetProveedoresById(int id);

        Task<bool> PostProveedores(Proveedores proveedores);

        Task<bool> PutProveedores(Proveedores proveedores);

        Task<bool> DeleteProveedores(Proveedores proveedores);

        Task<List<Proveedores>> FiltrarProveedores(ProveedorFiltroDTO filtro);
    }
}