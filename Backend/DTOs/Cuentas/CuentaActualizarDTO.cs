using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Cuentas
{
    public class CuentaActualizarDTO : CuentaCrearDTO
    {
        public int idCuenta { get; set; }
    }
}
