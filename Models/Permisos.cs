using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.Models
{
    public class Permisos
    {
        [Key]
        public int idPermiso { get; set; }

        [Required]
        [MaxLength(100)]
        public string nombrePermiso { get; set; } = string.Empty;

        public ICollection<RolesPermisos> rolesPermisos { get; set; }
            = new List<RolesPermisos>();
    }
}