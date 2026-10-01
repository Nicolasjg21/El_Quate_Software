using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Proveedores
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idProveedor { get; set; }

        [Required]
        public string nombreProveedor { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string telefono { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string direccion { get; set; } = string.Empty;

        public ICollection<Compras> compras { get; set; }
            = new List<Compras>();
    }
}
