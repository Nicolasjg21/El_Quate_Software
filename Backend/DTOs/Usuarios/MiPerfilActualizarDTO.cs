using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    /// <summary>Lo único que un usuario cambia de sí mismo además de su contraseña.</summary>
    public class MiPerfilActualizarDTO
    {
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres")]
        public string telefono { get; set; } = string.Empty;
    }
}
