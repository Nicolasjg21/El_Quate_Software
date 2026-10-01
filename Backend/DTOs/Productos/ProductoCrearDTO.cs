using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreProducto es obligatorio.")]
        [StringLength(200, ErrorMessage = "El campo nombreProducto no puede superar 200 caracteres.")]
        public string nombreProducto { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "El campo precioVenta debe estar entre 0 y 99999999.99.")]
        public decimal precioVenta { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El campo cantidadMinima no puede ser negativo.")]
        public int cantidadMinima { get; set; }

        public bool estado { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idCategoria debe ser mayor que cero.")]
        public int idCategoria { get; set; }
    }
}
