using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.DetalleCompras
{
    public class DetalleCompraActualizarDTO : DetalleCompraCrearDTO
    {
        [JsonRequired]
        public int idDetalleCompra { get; set; }
    }
}