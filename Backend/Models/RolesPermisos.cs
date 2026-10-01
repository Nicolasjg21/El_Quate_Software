namespace ElQuateDePatty.Models
{
    public class RolesPermisos
    {
        public int idRol { get; set; }

        public int idPermiso { get; set; }

        public Roles rol { get; set; } = null!;

        public Permisos permiso { get; set; } = null!;
    }
}