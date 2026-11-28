namespace ElQuateDePatty.DTOs.Comprobantes
{
    public class ComprobanteRespuestaDTO
    {
        public int idComprobante { get; set; }

        public int idCuenta { get; set; }

        public DateTime fecha { get; set; }

        public decimal total { get; set; }

        public int idMetodo { get; set; }

        /// <summary>Total sin el INC.</summary>
        public decimal baseGravable { get; set; }

        /// <summary>INC incluido en el total.</summary>
        public decimal impuesto { get; set; }

        /// <summary>Tarifa del INC aplicada (0,08).</summary>
        public decimal tasaImpuesto { get; set; }
    }
}
