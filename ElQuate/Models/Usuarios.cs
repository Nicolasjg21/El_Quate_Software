using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Usuarios
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idUsuario { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string nombres { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string apellidos { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string documento { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idTipoDocumento))]
        public int idTipoDocumento { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string telefono { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string passwordHash { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public bool estado { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public int idRol { get; set; }
    }
}