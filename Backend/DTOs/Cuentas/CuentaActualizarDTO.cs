using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Cuentas
{
    public class CuentaActualizarDTO : CuentaCrearDTO
    {
        [JsonRequired]
        public int idCuenta { get; set; }
    }
}