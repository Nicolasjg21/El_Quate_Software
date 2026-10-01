using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.DetalleCompras
{
    public class DetalleCompraActualizarDTO : DetalleCompraCrearDTO
    {
        public int idDetalleCompra { get; set; }
    }
}
