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

<<<<<<< HEAD
        [Required]
        public string nombres { get; set; } = string.Empty;

        [Required]
        public string apellidos { get; set; } = string.Empty;

        [Required]
=======
        [Required(ErrorMessage = "Campo requerido")]
        public string nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        public string apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
>>>>>>> 4be1777bf013c1739d08855c8a664c2d58b7bd14
        public string documento { get; set; } = string.Empty;

        [Required]
        public int idTipoDocumento { get; set; }

<<<<<<< HEAD
        [Required]
        public string telefono { get; set; } = string.Empty;

        [Required]
        public string passwordHash { get; set; } = string.Empty;
=======
        [Required(ErrorMessage = "Campo requerido")]
        public string telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo requerido")]
        public string? passwordHash { get; set; }
>>>>>>> 4be1777bf013c1739d08855c8a664c2d58b7bd14

        [Required]
        public bool estado { get; set; }

        [Required]
        public int idRol { get; set; }
<<<<<<< HEAD

        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        public TipoDocumento tipoDocumento { get; set; } = null!;

        public Roles rol { get; set; } = null!;

        public ICollection<Pedidos> pedidos { get; set; }
            = new List<Pedidos>();

        public ICollection<Kardex> movimientosKardex { get; set; }
            = new List<Kardex>();

        public ICollection<Auditorias> auditorias { get; set; }
            = new List<Auditorias>();
=======
        [Required(ErrorMessage = "Campo requerido")]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        public string email { get; set; } = string.Empty;
>>>>>>> 4be1777bf013c1739d08855c8a664c2d58b7bd14
    }
}