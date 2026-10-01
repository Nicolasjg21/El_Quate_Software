using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.TipoDocumento
{
    public class TipoDocumentoActualizarDTO : TipoDocumentoCrearDTO
    {
        public int idTipoDocumento { get; set; }
    }
}
