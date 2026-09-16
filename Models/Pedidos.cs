using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Pedidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idPedido { get; set; }

        [Required]
        public int idCuenta { get; set; }

        [Required]
        public int idUsuario { get; set; }

        [Required]
        public DateTime fecha { get; set; }

        [Required]
        [StringLength(20)]
        public string estadoPedido { get; set; } = string.Empty;

        public Cuentas cuenta { get; set; } = null!;

        public Usuarios usuario { get; set; } = null!;

        public ICollection<DetallePedidos> detalles { get; set; }
            = new List<DetallePedidos>();
    }
}