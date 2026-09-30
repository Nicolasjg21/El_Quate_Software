namespace ElQuateDePatty.DTOs.Compras
{
    public class CompraFiltroDTO
    {
        public DateTime? fechaDesde { get; set; }

        public DateTime? fechaHasta { get; set; }

        public int? idProveedor { get; set; }

        public int? totalArticulosMinimo { get; set; }

        public int? totalArticulosMaximo { get; set; }

        public decimal? costoTotalMinimo { get; set; }

        public decimal? costoTotalMaximo { get; set; }
    }
}