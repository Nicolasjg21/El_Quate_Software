using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace ElQuateSoftware.Models
{
    public class Compras
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idCompra { get; set; }
        [Required]
        [ForeignKey(nameof(idProveedor))]
        public int idProveedor { get; set; }
        [Required]
        public DateTime fecha { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal total { get; set; }
    }
}
