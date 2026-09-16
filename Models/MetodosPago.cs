using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class MetodosPago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMetodo { get; set; }

        [Required]
        [StringLength(50)]
        public string nombreMetodo { get; set; } = string.Empty;

        public ICollection<Comprobantes> comprobantes { get; set; }
            = new List<Comprobantes>();
    }
}