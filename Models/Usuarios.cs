using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Usuarios
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idUsuario { get; set; } 

        [Required(ErrorMessage = "Campo requerido")]
        public string nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        public string apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        public string documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idTipoDocumento))]
        public int idTipoDocumento { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        public string? passwordHash { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public bool estado { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public int idRol { get; set; }
        [Required(ErrorMessage = "Campo requerido")]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        public string email { get; set; } = string.Empty;
    }
}