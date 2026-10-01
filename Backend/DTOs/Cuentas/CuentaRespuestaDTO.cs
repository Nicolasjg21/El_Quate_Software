namespace ElQuateDePatty.DTOs.Cuentas
{
    public class CuentaRespuestaDTO
    {
        public int idCuenta { get; set; }

        public int idMesa { get; set; }

        public string estado { get; set; } = string.Empty;

        public DateTime fechaApertura { get; set; }

        public DateTime? fechaCierre { get; set; }

        public decimal total { get; set; }
    }
}
