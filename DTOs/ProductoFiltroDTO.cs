namespace ElQuateDePatty.DTOs
{
    public class ProductoFiltroDTO
    {
        public string? Nombre { get; set; }

        public int? IdCategoria { get; set; }

        public int? IdProveedor { get; set; }

        public decimal? CostoMinimo { get; set; }

        public decimal? CostoMaximo { get; set; }

        public decimal? PrecioVentaMinimo { get; set; }

        public decimal? PrecioVentaMaximo { get; set; }

        public bool? SinStock { get; set; }

        public bool? StockBajo { get; set; }
    }
}