namespace ElQuateDePatty.DTOs.Kardex
{
    public class KardexRespuestaDTO
    {
        public int idMovimiento { get; set; }

        public int idProducto { get; set; }

        public string tipoMovimiento { get; set; } = string.Empty;

        public int cantidad { get; set; }

        public int stockAnterior { get; set; }

        public int stockNuevo { get; set; }

        public string? motivo { get; set; }

        public DateTime fecha { get; set; }

        public int idUsuario { get; set; }
    }
}
