using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoActualizarDTO : ProductoCrearDTO
    {
        public int idProducto { get; set; }
    }
}
