using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Comprobantes
{
    public class ComprobanteCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idCuenta debe ser mayor que cero.")]
        public int idCuenta { get; set; }

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fecha debe contener una fecha válida.")]
        public DateTime fecha { get; set; }

        [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "El campo total debe estar entre 0 y 99999999.99.")]
        public decimal total { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idMetodo debe ser mayor que cero.")]
        public int idMetodo { get; set; }
    }
}
