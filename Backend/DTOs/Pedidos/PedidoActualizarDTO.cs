using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoActualizarDTO : PedidoCrearDTO
    {
        [JsonRequired]
        public int idPedido { get; set; }
    }
}