using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    public class CambiarPasswordDTO
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string passwordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        public string nuevaPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        public string confirmarPassword { get; set; } = string.Empty;
    }
}