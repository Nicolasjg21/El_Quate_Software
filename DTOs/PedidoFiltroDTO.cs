namespace ElQuateDePatty.DTOs
{
    public class PedidoFiltroDTO
    {
        public int? NumeroMesa { get; set; }

        public int? IdUsuario { get; set; }

        public DateTime? Fecha { get; set; }

        public TimeSpan? HoraInicio { get; set; }

        public TimeSpan? HoraFin { get; set; }

        public decimal? TotalMinimo { get; set; }

        public decimal? TotalMaximo { get; set; }

        public int? IdMetodoPago { get; set; }
    }
}