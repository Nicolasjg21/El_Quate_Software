using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Auditorias
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idAuditoria { get; set; }

        [Required]
        [StringLength(50)]
        public string tabla { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string accion { get; set; } = string.Empty;

        [Required]
        public int idUsuario { get; set; }

        public DateTime? fecha { get; set; }

        public string? datosAnteriores { get; set; }

        public string? datosNuevos { get; set; }

        public Usuarios usuario { get; set; } = null!;
    }
}
