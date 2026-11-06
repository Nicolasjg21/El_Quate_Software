using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Permisos
{
    public class PermisoActualizarDTO : PermisoCrearDTO
    {
        [JsonRequired]
        public int idPermiso { get; set; }
    }
}