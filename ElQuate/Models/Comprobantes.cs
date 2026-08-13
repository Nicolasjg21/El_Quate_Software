using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Comprobantes
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idComprobante { get; set; }
        [Required]
        [ForeignKey(nameof(idCuenta))]
        public int idCuenta { get; set; }
        [Required]
        public DateTime fecha { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal total { get; set; }
        [Required]
        [ForeignKey(nameof(idMetodo))]
        public int idMetodo { get; set; }
    }
}