using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Proveedores
{
    public class ProveedorCrearDTO
    {
        [Required(ErrorMessage = "El campo nombreProveedor es obligatorio.")]
        [StringLength(200, ErrorMessage = "El campo nombreProveedor no puede superar 200 caracteres.")]
        public string nombreProveedor { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo telefono es obligatorio.")]
        [StringLength(20, ErrorMessage = "El campo telefono no puede superar 20 caracteres.")]
        public string telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo direccion es obligatorio.")]
        [StringLength(150, ErrorMessage = "El campo direccion no puede superar 150 caracteres.")]
        public string direccion { get; set; } = string.Empty;
    }
}
