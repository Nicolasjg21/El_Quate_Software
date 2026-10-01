using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.TipoDocumento
{
    public class TipoDocumentoCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreTipo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo nombreTipo no puede superar 100 caracteres.")]
        public string nombreTipo { get; set; } = string.Empty;
    }
}
