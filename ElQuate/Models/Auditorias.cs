using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Auditorias
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idAuditoria { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "El nombre de la tabla no puede exceder los 50 caracteres")]
        public string tabla { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "El nombre de la acción no puede exceder los 50 caracteres")]
        public string accion { get; set; }
        [Required]
        [ForeignKey(nameof(idUsuario))]
        public int idUsuario { get; set; }
        public DateTime? fecha { get; set; }
        public string? datosAnteriores { get; set; }
        public string? datosNuevos { get; set; }
    }
}
