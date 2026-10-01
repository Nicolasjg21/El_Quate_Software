using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Mesas
{
    public class MesaFiltroDTO
    {
        [StringLength(20, ErrorMessage = "El campo estado no puede superar 20 caracteres.")]
        public string? estado { get; set; }
    }
}