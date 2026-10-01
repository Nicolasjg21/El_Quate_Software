using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Models
{
    public class DetallePedidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idDetalle { get; set; }

        [Required]
        public int idPedido { get; set; }

        [Required]
        public int idProducto { get; set; }

        [Required]
        public int cantidad { get; set; }

        [Required]
        [Precision(10, 2)]
        public decimal precioUnitario { get; set; }

        public Pedidos pedido { get; set; } = null!;

        public Productos producto { get; set; } = null!;
    }
}