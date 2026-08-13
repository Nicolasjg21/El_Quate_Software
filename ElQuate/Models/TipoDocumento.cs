using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class TipoDocumento

    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idTipoDocumento { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string nombreTipo { get; set; }
    }
}