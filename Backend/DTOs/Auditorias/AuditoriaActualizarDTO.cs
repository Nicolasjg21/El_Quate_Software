using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Auditorias
{
    public class AuditoriaActualizarDTO : AuditoriaCrearDTO
    {
        public int idAuditoria { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idUsuario debe ser mayor que cero.")]
        public int idUsuario { get; set; }
    }
}
