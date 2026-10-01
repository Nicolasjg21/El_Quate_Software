using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Proveedores
{
    public class ProveedorActualizarDTO : ProveedorCrearDTO
    {
        public int idProveedor { get; set; }
    }
}
