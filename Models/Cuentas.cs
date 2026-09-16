using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ElQuateDePatty.Models
{
    public class Cuentas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idCuenta { get; set; }

        [Required]
        public int idMesa { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; } = string.Empty;

        [Required]
        public DateTime fechaApertura { get; set; }

        public DateTime? fechaCierre { get; set; }

        [Required]
        [Precision(10, 2)]
        public decimal total { get; set; }

        public Mesas mesa { get; set; } = null!;

        public ICollection<Pedidos> pedidos { get; set; }
            = new List<Pedidos>();

        public ICollection<Comprobantes> comprobantes { get; set; }
            = new List<Comprobantes>();
    }
}