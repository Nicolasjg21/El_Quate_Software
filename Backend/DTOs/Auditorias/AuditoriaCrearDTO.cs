using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Auditorias
{
    public class AuditoriaCrearDTO
    {
        [Required(ErrorMessage = "El campo tabla es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo tabla no puede superar 50 caracteres.")]
        public string tabla { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo accion es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo accion no puede superar 50 caracteres.")]
        public string accion { get; set; } = string.Empty;

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fecha debe contener una fecha válida.")]
        public DateTime? fecha { get; set; }

        [StringLength(20000, ErrorMessage = "El campo datosAnteriores no puede superar 20000 caracteres.")]
        public string? datosAnteriores { get; set; }

        [StringLength(20000, ErrorMessage = "El campo datosNuevos no puede superar 20000 caracteres.")]
        public string? datosNuevos { get; set; }
    }
}
