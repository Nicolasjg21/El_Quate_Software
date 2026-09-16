using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class TipoDocumento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idTipoDocumento { get; set; }

        [Required]
        public string nombreTipo { get; set; } = string.Empty;

        public ICollection<Usuarios> usuarios { get; set; }
            = new List<Usuarios>();
    }
}