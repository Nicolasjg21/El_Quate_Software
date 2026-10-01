using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoActualizarDTO : PedidoCrearDTO
    {
        public int idPedido { get; set; }
    }
}
