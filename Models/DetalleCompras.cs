using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Models
{
    public class DetalleCompras
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idDetalleCompra { get; set; }

        [Required]
        public int idCompra { get; set; }

        [Required]
        public int idProducto { get; set; }

        [Required]
        public int cantidad { get; set; }

        [Required]
        [Precision(10, 2)]
        public decimal precioCompra { get; set; }

        public Compras compra { get; set; } = null!;

        public Productos producto { get; set; } = null!;
    }
}