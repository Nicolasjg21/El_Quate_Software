using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    public class CambiarPasswordDTO
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        [StringLength(128, ErrorMessage = "La contraseña actual no puede superar 128 caracteres")]
        public string passwordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "La nueva contraseña debe tener entre 8 y 128 caracteres")]
        public string nuevaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [StringLength(128, ErrorMessage = "La confirmación no puede superar 128 caracteres")]
        public string confirmarPassword { get; set; } = string.Empty;
    }
}