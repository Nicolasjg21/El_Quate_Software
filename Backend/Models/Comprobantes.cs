using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Comprobantes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idComprobante { get; set; }

        [Required]
        public int idCuenta { get; set; }

        [Required]
        public DateTime fecha { get; set; }

        [Required]
        [Precision(10, 2)]
        public decimal total { get; set; }

        [Required]
        public int idMetodo { get; set; }

        /// <summary>Total sin el INC (total / 1,08, en pesos enteros). Ver ImpuestoConsumo.</summary>
        [Precision(10, 2)]
        public decimal baseGravable { get; set; }

        /// <summary>INC incluido en el total (total − baseGravable).</summary>
        [Precision(10, 2)]
        public decimal impuesto { get; set; }

        /// <summary>Tarifa del INC con que se emitió el comprobante (0,08).</summary>
        [Precision(5, 4)]
        public decimal tasaImpuesto { get; set; }

        public Cuentas cuenta { get; set; } = null!;

        public MetodosPago metodoPago { get; set; } = null!;
    }
}