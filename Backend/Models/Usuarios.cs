using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Usuarios
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idUsuario { get; set; }

        [Required]
        public string nombres { get; set; } = string.Empty;

        [Required]
        public string apellidos { get; set; } = string.Empty;

        [Required]
        public string documento { get; set; } = string.Empty;

        [Required]
        public int idTipoDocumento { get; set; }

        [Required]
        public string telefono { get; set; } = string.Empty;

        [Required]
        public string passwordHash { get; set; } = string.Empty;

        [Required]
        public bool estado { get; set; }

        [Required]
        public int idRol { get; set; }

        [Required]
        [EmailAddress]
        public string email { get; set; } = string.Empty;

        /// <summary>true mientras tenga una contraseña temporal asignada por la administración.</summary>
        public bool debeCambiarPassword { get; set; }

        /// <summary>Vencimiento de la contraseña temporal, en UTC. Null si no hay una vigente.</summary>
        public DateTime? passwordTemporalExpira { get; set; }

        // Relaciones
        public TipoDocumento tipoDocumento { get; set; } = null!;

        public Roles rol { get; set; } = null!;

        public ICollection<Pedidos> pedidos { get; set; }
            = new List<Pedidos>();

        public ICollection<Kardex> movimientosKardex { get; set; }
            = new List<Kardex>();

        public ICollection<Auditorias> auditorias { get; set; }
            = new List<Auditorias>();
    }
}