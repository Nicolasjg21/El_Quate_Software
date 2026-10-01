using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class Categorias
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idCategoria { get; set; }

        [Required]
        [StringLength(100)]
        public string nombreCategoria { get; set; } = string.Empty;

        public ICollection<Productos> productos { get; set; }
            = new List<Productos>();
    }
}