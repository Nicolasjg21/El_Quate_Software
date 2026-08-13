using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElQuateSoftware.Models
{
    public class Productos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idProducto { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public string nombreProducto { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public decimal precioVenta { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public int cantidadMinima { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        public bool estado { get; set; }

        [Required(ErrorMessage = "Campo requerido")]
        [ForeignKey(nameof(idCategoria))]
        public int idCategoria { get; set; }
    }
}