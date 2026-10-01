using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Categorias
{
    public class CategoriaActualizarDTO : CategoriaCrearDTO
    {
        public int idCategoria { get; set; }
    }
}
