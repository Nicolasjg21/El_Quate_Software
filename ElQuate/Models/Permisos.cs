using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Permisos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idPermiso { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [StringLength(100, ErrorMessage = "Solo se admiten 100 caracteres")]
        public string nombrePermiso { get; set; }

    }
}