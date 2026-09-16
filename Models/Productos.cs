using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Productos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idProducto { get; set; }

        [Required]
        public string nombreProducto { get; set; } = string.Empty;

        [Required]
        [Precision(10, 2)]
        public decimal precioVenta { get; set; }

        [Required]
        public int cantidadMinima { get; set; }

        [Required]
        public bool estado { get; set; }

        [Required]
        public int idCategoria { get; set; }

        public Categorias categoria { get; set; } = null!;

        public ICollection<DetalleCompras> detallesCompras { get; set; }
            = new List<DetalleCompras>();

        public ICollection<DetallePedidos> detallesPedidos { get; set; }
            = new List<DetallePedidos>();

        public ICollection<Kardex> movimientosKardex { get; set; }
            = new List<Kardex>();
    }
}