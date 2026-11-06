using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.DetallePedidos
{
    public class DetallePedidoActualizarDTO : DetallePedidoCrearDTO
    {
        [JsonRequired]
        public int idDetalle { get; set; }
    }
}