using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>Costo unitario con que se valora el movimiento (compra en una ENTRADA; promedio vigente en lo demás).</summary>
        [Precision(12, 2)]
        public decimal costoUnitario { get; set; }

        /// <summary>Costo promedio ponderado del producto después del movimiento.</summary>
        [Precision(12, 2)]
        public decimal costoPromedio { get; set; }

        /// <summary>
        /// Solo para registrar: costo de compra que trae una ENTRADA. No se guarda; el repositorio lo usa
        /// para calcular costoUnitario y costoPromedio (ver CostoPromedio).
        /// </summary>
        [NotMapped]
        public decimal? costoEntrada { get; set; }

        public Productos producto { get; set; } = null!;

        public Usuarios usuario { get; set; } = null!;
    }
}