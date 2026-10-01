namespace ElQuateDePatty.DTOs.Comprobantes
{
    public class ComprobanteRespuestaDTO
    {
        public int idComprobante { get; set; }

        public int idCuenta { get; set; }

        public DateTime fecha { get; set; }

        public decimal total { get; set; }

        public int idMetodo { get; set; }
    }
}
