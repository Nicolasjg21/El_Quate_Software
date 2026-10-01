using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Mesas
{
    public class MesaActualizarDTO : MesaCrearDTO
    {
        public int idMesa { get; set; }
    }
}
