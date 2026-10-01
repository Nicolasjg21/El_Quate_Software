using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Categorias
{
    public class CategoriaCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreCategoria es obligatorio.")]
        [StringLength(100, ErrorMessage = "El campo nombreCategoria no puede superar 100 caracteres.")]
        public string nombreCategoria { get; set; } = string.Empty;
    }
}
