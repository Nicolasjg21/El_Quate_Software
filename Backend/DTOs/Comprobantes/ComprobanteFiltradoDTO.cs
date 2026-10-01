using ElQuateDePatty.DTOs.MetodosPago;

namespace ElQuateDePatty.DTOs.Comprobantes
{
    public class ComprobanteFiltradoDTO : ComprobanteRespuestaDTO
    {
        public MetodoPagoRespuestaDTO? metodoPago { get; set; }
    }
}
