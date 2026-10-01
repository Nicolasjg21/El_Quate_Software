using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.RolesPermisos
{
    public class RolPermisoCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idRol debe ser mayor que cero.")]
        public int idRol { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idPermiso debe ser mayor que cero.")]
        public int idPermiso { get; set; }
    }
}
