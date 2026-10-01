using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.DetallePedidos
{
    public class DetallePedidoCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idPedido debe ser mayor que cero.")]
        public int idPedido { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idProducto debe ser mayor que cero.")]
        public int idProducto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo cantidad debe ser mayor que cero.")]
        public int cantidad { get; set; }

        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo precioUnitario debe estar entre 0 y 99999999.99.")]
        public decimal precioUnitario { get; set; }
    }
}
