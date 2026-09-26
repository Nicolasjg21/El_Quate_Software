namespace ElQuateDePatty.DTOs
{
    public class UsuarioCrearDTO
    {
        public string nombres { get; set; } = string.Empty;

        public string apellidos { get; set; } = string.Empty;

        public string documento { get; set; } = string.Empty;

        public int idTipoDocumento { get; set; }

        public string telefono { get; set; } = string.Empty;

        public string email { get; set; } = string.Empty;

        public string password { get; set; } = string.Empty;

        public bool estado { get; set; }

        public int idRol { get; set; }
    }
}