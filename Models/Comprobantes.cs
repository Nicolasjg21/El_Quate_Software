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

        public Cuentas cuenta { get; set; } = null!;

        public MetodosPago metodoPago { get; set; } = null!;
    }
}