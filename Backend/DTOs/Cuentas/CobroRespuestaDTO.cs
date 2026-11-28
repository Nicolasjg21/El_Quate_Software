namespace ElQuateDePatty.DTOs.Cuentas
{
    /// <summary>Resultado de un cobro: el comprobante emitido y los datos de la cuenta cerrada.</summary>
    public class CobroRespuestaDTO
    {
        public int idComprobante { get; set; }

        public int idCuenta { get; set; }

        public int numeroMesa { get; set; }

        public int idMetodo { get; set; }

        /// <summary>Fecha del comprobante (la del servidor).</summary>
        public DateTime fecha { get; set; }

        public DateTime fechaApertura { get; set; }

        public DateTime? fechaCierre { get; set; }

        /// <summary>Total cobrado, con el INC incluido.</summary>
        public decimal total { get; set; }

        public decimal baseGravable { get; set; }

        public decimal impuesto { get; set; }

        public decimal tasaImpuesto { get; set; }
    }
}
