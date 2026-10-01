namespace ElQuateDePatty.DTOs.Compras
{
    public class CompraRespuestaDTO
    {
        public int idCompra { get; set; }

        public int idProveedor { get; set; }

        public DateTime fecha { get; set; }

        public decimal total { get; set; }
    }
}
