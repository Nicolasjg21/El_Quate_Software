using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace ElQuateDePatty.Models
{
    public class Kardex
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMovimiento { get; set; }

        [Required]
        public int idProducto { get; set; }

        [Required]
        [StringLength(10)]
        public string tipoMovimiento { get; set; } = string.Empty;

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
        public int idUsuario { get; set; }

        public Productos producto { get; set; } = null!;

        public Usuarios usuario { get; set; } = null!;
    }
}