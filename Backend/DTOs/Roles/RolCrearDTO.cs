using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Roles
{
    public class RolCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreRol es obligatorio.")]
        [StringLength(50, ErrorMessage = "El campo nombreRol no puede superar 50 caracteres.")]
        public string nombreRol { get; set; } = string.Empty;
    }
}
