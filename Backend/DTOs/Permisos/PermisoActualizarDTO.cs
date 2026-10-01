using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Permisos
{
    public class PermisoActualizarDTO : PermisoCrearDTO
    {
        public int idPermiso { get; set; }
    }
}
