using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface IUsuariosRepository
    {
        Task<List<Usuarios>> GetUsuarios();

        Task<Usuarios?> GetUsuariosById(int id);

        Task<bool> PostUsuarios(Usuarios usuarios);

        Task<bool> PutUsuarios(Usuarios usuarios);

        Task<bool> DeleteUsuarios(Usuarios usuarios);

        Task<Usuarios?> GetUsuariosByEmail(string email);

        Task<bool> ExisteEmail(string email, int? idUsuarioExcluido = null);

        Task<Usuarios?> ObtenerUsuarioActivoParaSesion(int idUsuario);

        /// <summary>Crea el usuario y su registro de auditoría en una sola transacción (un solo SaveChanges).</summary>
        Task<bool> CrearConAuditoria(Usuarios usuario, Auditorias auditoria);

        /// <summary>Guarda los cambios del usuario y su registro de auditoría en una sola transacción.</summary>
        Task<bool> ActualizarConAuditoria(Usuarios usuario, Auditorias auditoria);

        /// <summary>Elimina el usuario y registra la auditoría en una sola transacción.</summary>
        Task<bool> EliminarConAuditoria(Usuarios usuario, Auditorias auditoria);
    }
}