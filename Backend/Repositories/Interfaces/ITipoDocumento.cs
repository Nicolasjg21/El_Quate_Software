using ElQuateDePatty.Models;

namespace ElQuateDePatty.Repositories.Interfaces
{
    public interface ITipoDocumentoRepository
    {
        Task<List<TipoDocumento>> GetTipoDocumento();

        Task<TipoDocumento?> GetTipoDocumentoById(int id);

        Task<bool> PostTipoDocumento(TipoDocumento tipoDocumento);

        Task<bool> PutTipoDocumento(TipoDocumento tipoDocumento);

        Task<bool> DeleteTipoDocumento(TipoDocumento tipoDocumento);
    }
}