namespace ElQuateDePatty.Services
{
    public interface IPermisosRolService
    {
        Task<bool> TienePermiso(int idRol, string permiso);
    }
}
