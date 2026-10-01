namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoDetalleRespuestaDTO
    {
        public int idProducto { get; set; }

        public string nombreProducto { get; set; } = string.Empty;

        public decimal precioVenta { get; set; }

        public int cantidadMinima { get; set; }

        public bool estado { get; set; }

        public int idCategoria { get; set; }
    }
}
