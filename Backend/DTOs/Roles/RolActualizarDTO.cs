using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Roles
{
    public class RolActualizarDTO : RolCrearDTO
    {
        [JsonRequired]
        public int idRol { get; set; }
    }
}