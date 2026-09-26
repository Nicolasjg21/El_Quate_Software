namespace ElQuateDePatty.DTOs
{
    public class CompraFiltroDTO
    {
        public DateTime? FechaDesde { get; set; }

        public DateTime? FechaHasta { get; set; }

        public int? IdProveedor { get; set; }

        public int? TotalArticulosMinimo { get; set; }

        public int? TotalArticulosMaximo { get; set; }

        public decimal? CostoTotalMinimo { get; set; }

        public decimal? CostoTotalMaximo { get; set; }
    }
}