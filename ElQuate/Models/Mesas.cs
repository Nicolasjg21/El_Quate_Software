using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Mesas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idMesa { get; set; }
        [Required]
        public int numeroMesa { get; set; }
        [Required]
        [StringLength(20, ErrorMessage = "El estado de la mesa no puede exceder los 20 caracteres.")]
        public string estado { get; set; }
    }
}