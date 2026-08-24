using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateDePatty.Models
{
    public class RolesPermisos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idRol { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idPermiso))]
        public int idPermiso { get; set; }
    }
}
