using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Proveedores
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idProveedor { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string nombreProveedor { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [StringLength(20, ErrorMessage = "El nombre de la acción no puede exceder los 50 caracteres")]
        public string telefono { get; set; }
        [Required(ErrorMessage = "Campo requerido")]
        [StringLength(150, ErrorMessage = "El nombre de la acción no puede exceder los 150 caracteres")]
        public string direccion { get; set; }
    }
}
