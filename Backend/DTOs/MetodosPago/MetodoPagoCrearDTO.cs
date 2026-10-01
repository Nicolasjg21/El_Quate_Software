using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.MetodosPago
{
    public class MetodoPagoCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreMetodo es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo nombreMetodo no puede superar 50 caracteres.")]
        public string nombreMetodo { get; set; } = string.Empty;
    }
}
