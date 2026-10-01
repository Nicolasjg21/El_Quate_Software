using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.Models
{
    public class Roles
    {
        [Key]
        public int idRol { get; set; }

        [Required]
        [MaxLength(50)]
        public string nombreRol { get; set; } = string.Empty;

        public ICollection<Usuarios> usuarios { get; set; }
            = new List<Usuarios>();

        public ICollection<RolesPermisos> rolesPermisos { get; set; }
            = new List<RolesPermisos>();
    }
}