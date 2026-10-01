namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoRespuestaDTO
    {
        public int idPedido { get; set; }

        public int idCuenta { get; set; }

        public int idUsuario { get; set; }

        public DateTime fecha { get; set; }

        public string estadoPedido { get; set; } = string.Empty;
    }
}
