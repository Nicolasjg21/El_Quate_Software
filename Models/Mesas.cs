using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Mesas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMesa { get; set; }

        [Required]
        public int numeroMesa { get; set; }

        [Required]
        [StringLength(20)]
        public string estado { get; set; } = string.Empty;

        public ICollection<Cuentas> cuentas { get; set; }
            = new List<Cuentas>();
    }
}