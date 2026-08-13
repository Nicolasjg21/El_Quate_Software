using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Pedidos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idPedido { get; set; }
        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idCuenta))]
        public int idCuenta { get; set; }
        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idUsuario))]
        public int idUsuario { get; set; }
        [Required (ErrorMessage ="Campo requerido")]
        public DateTime fecha { get; set; }
        [Required(ErrorMessage = "Campo requerido")]
        [MaxLength(20)]
        public string estadoPedido { get; set; }

    }
}