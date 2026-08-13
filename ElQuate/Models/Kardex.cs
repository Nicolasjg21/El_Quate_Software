using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ElQuateSoftware.Models
{
    public class Kardex
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMovimiento { get; set; }
        [Required]
        [ForeignKey(nameof(idProducto))]
        public int idProducto { get; set; }
        [Required]
        [StringLength(10, ErrorMessage = "El tipo de movimiento no puede exceder los 10 caracteres.")]
        public string tipoMovimiento { get; set; }
        [Required]
        public int cantidad { get; set; }
        [Required]
        public int stockAnterior { get; set; }
        [Required]
        public int stockNuevo { get; set; }
        public string? motivo { get; set; }
        [Required]
        public DateTime fecha { get; set; }
        [Required]
        [ForeignKey(nameof(idUsuario))]
        public int idUsuario { get; set; }
    }
}