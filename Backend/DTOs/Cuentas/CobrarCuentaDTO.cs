using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Cuentas
{
    /// <summary>
    /// Solicitud de cobro de una cuenta. Solo indica qué cuenta y con qué método pagó el cliente:
    /// el total, la base, el INC y las fechas los calcula el servidor con lo que hay en la base.
    /// </summary>
    public class CobrarCuentaDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idCuenta debe ser mayor que cero.")]
        public int idCuenta { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El campo idMetodo debe ser mayor que cero.")]
        public int idMetodo { get; set; }

        /// <summary>
        /// Total que vio quien cobra. Si la cuenta cambió mientras tanto (alguien agregó o quitó
        /// productos), el servidor no cobra y responde 409 con el total actual. Es opcional.
        /// </summary>
        [Range(typeof(decimal), "0", "99999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El campo totalEsperado debe estar entre 0 y 99999999.99.")]
        public decimal? totalEsperado { get; set; }
    }
}
