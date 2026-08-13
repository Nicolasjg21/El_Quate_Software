using ElQuateSoftware.Models;

namespace ElQuateSoftwareSoftware.Repositories.Interfaces
{
    public interface ITipoDocumentoRepository
    {
        Task<List<TipoDocumento>> GetTipoDocumento();

        Task<bool> PostTipoDocumento(TipoDocumento tipoDocumento);
    }
}