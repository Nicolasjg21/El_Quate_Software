using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Comprobantes
{
    public class ComprobanteActualizarDTO : ComprobanteCrearDTO
    {
        public int idComprobante { get; set; }
    }
}
