using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateSoftware.Models
{
    public class DetalleCompras
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idDetalleCompra { get; set; }
        [Required]
        [ForeignKey(nameof(idCompra))]
        public int idCompra { get; set; }
        [Required]
        [ForeignKey(nameof(idProducto))]
        public int idProducto { get; set; }
        [Required]
        public int cantidad { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal precioCompra { get; set; }
    }
}