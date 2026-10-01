using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Pedidos
{
    public class PedidoCrearDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idCuenta debe ser mayor que cero.")]
        public int idCuenta { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idUsuario debe ser mayor que cero.")]
        public int idUsuario { get; set; }

        [Range(typeof(DateTime), "2000-01-01", "2100-12-31", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo fecha debe contener una fecha válida.")]
        public DateTime fecha { get; set; }

        [Required(ErrorMessage = "El campo estadoPedido es obligatorio.")]
        [StringLength(20, ErrorMessage = "El campo estadoPedido no puede superar 20 caracteres.")]
        public string estadoPedido { get; set; } = string.Empty;
    }
}
