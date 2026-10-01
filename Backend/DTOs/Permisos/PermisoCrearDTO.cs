using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Permisos
{
    public class PermisoCrearDTO
    {
        [Required(ErrorMessage = "El campo nombrePermiso es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo nombrePermiso no puede superar 100 caracteres.")]
        public string nombrePermiso { get; set; } = string.Empty;
    }
}
