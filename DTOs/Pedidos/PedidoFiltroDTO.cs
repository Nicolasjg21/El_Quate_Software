namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoFiltroDTO
    {
        public int? numeroMesa { get; set; }

        public int? idUsuario { get; set; }

        public DateTime? fecha { get; set; }

        public TimeSpan? horaInicio { get; set; }

        public TimeSpan? horaFin { get; set; }

        public decimal? totalMinimo { get; set; }

        public decimal? totalMaximo { get; set; }

        public int? idMetodoPago { get; set; }
    }
}