using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Productos
{
    public class ProductoActualizarDTO : ProductoCrearDTO
    {
        [JsonRequired]
        public int idProducto { get; set; }
    }
}