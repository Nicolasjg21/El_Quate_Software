using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.MetodosPago
{
    public class MetodoPagoActualizarDTO : MetodoPagoCrearDTO
    {
        [JsonRequired]
        public int idMetodo { get; set; }
    }
}