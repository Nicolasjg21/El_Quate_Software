namespace ElQuateDePatty.DTOs.DetalleCompras
{
    public class DetalleCompraRespuestaDTO
    {
        public int idDetalleCompra { get; set; }

        public int idCompra { get; set; }

        public int idProducto { get; set; }

        public int cantidad { get; set; }

        public decimal precioCompra { get; set; }
    }
}
