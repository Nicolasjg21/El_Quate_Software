namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoFiltroDTO
    {
        public string? nombre { get; set; }

        public int? idCategoria { get; set; }

        public int? idProveedor { get; set; }

        public decimal? costoMinimo { get; set; }

        public decimal? costoMaximo { get; set; }

        public decimal? precioVentaMinimo { get; set; }

        public decimal? precioVentaMaximo { get; set; }

        public bool? sinStock { get; set; }

        public bool? stockBajo { get; set; }
    }
}