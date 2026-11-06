using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.TipoDocumento
{
    public class TipoDocumentoActualizarDTO : TipoDocumentoCrearDTO
    {
        [JsonRequired]
        public int idTipoDocumento { get; set; }
    }
}