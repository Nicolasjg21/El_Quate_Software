using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Usuarios
{
    public class UsuarioActualizarDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es obligatorio")]
        public string documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de documento es obligatorio")]
        public int idTipoDocumento { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        public bool estado { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public int idRol { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        public string email { get; set; } = string.Empty;
    }
}