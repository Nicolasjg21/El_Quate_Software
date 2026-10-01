using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.DetallePedidos
{
    public class DetallePedidoActualizarDTO : DetallePedidoCrearDTO
    {
        public int idDetalle { get; set; }
    }
}
