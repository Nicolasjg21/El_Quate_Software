namespace ElQuateDePatty.DTOs.DetallePedidos
{
    public class DetallePedidoRespuestaDTO
    {
        public int idDetalle { get; set; }

        public int idPedido { get; set; }

        public int idProducto { get; set; }

        public int cantidad { get; set; }

        public decimal precioUnitario { get; set; }
    }
}
