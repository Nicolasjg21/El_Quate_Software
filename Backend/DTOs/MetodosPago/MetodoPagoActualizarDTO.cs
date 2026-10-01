using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.MetodosPago
{
    public class MetodoPagoActualizarDTO : MetodoPagoCrearDTO
    {
        public int idMetodo { get; set; }
    }
}
