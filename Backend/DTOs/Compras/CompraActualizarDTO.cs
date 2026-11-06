using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Compras
{
    public class CompraActualizarDTO : CompraCrearDTO
    {
        [JsonRequired]
        public int idCompra { get; set; }
    }
}