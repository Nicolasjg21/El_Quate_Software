using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Proveedores
{
    public class ProveedorActualizarDTO : ProveedorCrearDTO
    {
        [JsonRequired]
        public int idProveedor { get; set; }
    }
}