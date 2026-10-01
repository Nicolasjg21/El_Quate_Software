using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Cuentas
{
    public class CuentaCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idMesa debe ser mayor que cero.")]
        public int idMesa { get; set; }

        [Required(ErrorMessage = "El campo estado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El campo estado no puede superar 20 caracteres.")]
        public string estado { get; set; } = string.Empty;

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fechaApertura debe contener una fecha vÃ¡lida.")]
        public DateTime fechaApertura { get; set; }

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fechaCierre debe contener una fecha vÃ¡lida.")]
        public DateTime? fechaCierre { get; set; }

        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo total debe estar entre 0 y 99999999.99.")]
        public decimal total { get; set; }
    }
}
