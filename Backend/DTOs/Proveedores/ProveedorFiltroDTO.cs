using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Proveedores
{
    public class ProveedorFiltroDTO
    {
        [StringLength(200, ErrorMessage = "El campo nombreProveedor no puede superar 200 caracteres.")]
        public string? nombreProveedor { get; set; }

        [StringLength(20, ErrorMessage = "El campo telefono no puede superar 20 caracteres.")]
        public string? telefono { get; set; }

        [StringLength(150, ErrorMessage = "El campo direccion no puede superar 150 caracteres.")]
        public string? direccion { get; set; }
    }
}