using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Kardex
{
    public class KardexActualizarDTO : KardexCrearDTO
    {
        public int idMovimiento { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idUsuario debe ser mayor que cero.")]
        public int idUsuario { get; set; }
    }
}
