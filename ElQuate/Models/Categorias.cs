using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Categorias
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idCategoria { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El nombre de la categoría no puede exceder los 100 caracteres")]
        public string nombreCategoria { get; set; }
    }
}