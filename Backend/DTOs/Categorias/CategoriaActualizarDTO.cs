using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Categorias
{
    public class CategoriaActualizarDTO : CategoriaCrearDTO
    {
        [JsonRequired]
        public int idCategoria { get; set; }
    }
}