namespace ElQuateDePatty.DTOs.Auditorias
{
    public class AuditoriaRespuestaDTO
    {
        public int idAuditoria { get; set; }

        public string tabla { get; set; } = string.Empty;

        public string accion { get; set; } = string.Empty;

        public int idUsuario { get; set; }

        public DateTime? fecha { get; set; }

        public string? datosAnteriores { get; set; }

        public string? datosNuevos { get; set; }
    }
}
