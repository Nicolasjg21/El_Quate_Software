using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Kardex
{
    public class KardexCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idProducto debe ser mayor que cero.")]
        public int idProducto { get; set; }

        [Required(ErrorMessage = "El campo tipoMovimiento es obligatorio.")]
        [StringLength(10, ErrorMessage = "El campo tipoMovimiento no puede superar 10 caracteres.")]
        public string tipoMovimiento { get; set; } = string.Empty;

        public int cantidad { get; set; }

        public int stockAnterior { get; set; }

        public int stockNuevo { get; set; }

        [StringLength(500, ErrorMessage = "El campo motivo no puede superar 500 caracteres.")]
        public string? motivo { get; set; }

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fecha debe contener una fecha válida.")]
        public DateTime fecha { get; set; }
    }
}
