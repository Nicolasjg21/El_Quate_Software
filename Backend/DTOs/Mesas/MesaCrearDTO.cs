using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Mesas
{
    public class MesaCrearDTO
    {
        [Range(0, int.MaxValue, ErrorMessage = "El campo numeroMesa no puede ser negativo.")]
        public int numeroMesa { get; set; }

        [Required(ErrorMessage = "El campo estado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El campo estado no puede superar 20 caracteres.")]
        public string estado { get; set; } = string.Empty;
    }
}
