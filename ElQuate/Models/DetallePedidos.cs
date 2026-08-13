using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateSoftware.Models
{
    public class DetallePedidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idDetalle { get; set; }
        [Required]
        [ForeignKey(nameof(idPedido))]
        public int idPedido { get; set; }
        [Required]
        [ForeignKey(nameof(idProducto))]
        public int idProducto { get; set; }
        [Required]
        public int cantidad { get; set; }
        [Required]
        [Precision(10, 2)]
        public decimal precioUnitario { get; set; }
    }
}