using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Mesas
{
    public class MesaActualizarDTO : MesaCrearDTO
    {
        [JsonRequired]
        public int idMesa { get; set; }
    }
}