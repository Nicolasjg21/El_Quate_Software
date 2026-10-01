namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoRespuestaDTO
    {
        public int idProducto { get; set; }

        public string? nombreProducto { get; set; }

        public decimal precioVenta { get; set; }

        public decimal? costoPromedio { get; set; }

        public int stockActual { get; set; }

        public bool stockBajo { get; set; }

        public string? categoria { get; set; }

        public string? proveedor { get; set; }
    }
}
