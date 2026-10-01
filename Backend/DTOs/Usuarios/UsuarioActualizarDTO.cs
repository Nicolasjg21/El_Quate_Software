using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    public class UsuarioActualizarDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres")]
        public string nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(200, ErrorMessage = "El apellido no puede superar 200 caracteres")]
        public string apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es obligatorio")]
        [StringLength(50, ErrorMessage = "El documento no puede superar 50 caracteres")]
        public string documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El tipo de documento no es válido")]
        public int idTipoDocumento { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres")]
        public string telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        public bool estado { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El rol no es válido")]
        public int idRol { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(254, ErrorMessage = "El correo electrónico no puede superar 254 caracteres")]
        public string email { get; set; } = string.Empty;
    }
}