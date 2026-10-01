namespace ElQuateDePatty.DTOs.Proveedores
{
    public class ProveedorRespuestaDTO
    {
        public int idProveedor { get; set; }

        public string nombreProveedor { get; set; } = string.Empty;

        public string telefono { get; set; } = string.Empty;

        public string direccion { get; set; } = string.Empty;
    }
}
