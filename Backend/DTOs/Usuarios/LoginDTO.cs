using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        [StringLength(254, ErrorMessage = "El correo electrónico no puede superar 254 caracteres")]
        public string email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres")]
        public string password { get; set; } = string.Empty;
    }
}
