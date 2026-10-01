using ElQuateDePatty.DTOs.DetallePedidos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class DetallePedidosMapper
    {
        public static DetallePedidos ToEntity(this DetallePedidoCrearDTO dto)
        {
            return new DetallePedidos
            {
                idPedido = dto.idPedido,
                idProducto = dto.idProducto,
                cantidad = dto.cantidad,
                precioUnitario = dto.precioUnitario
            };
        }

        public static DetallePedidoRespuestaDTO ToRespuestaDTO(this DetallePedidos entidad)
        {
            return new DetallePedidoRespuestaDTO
            {
                idDetalle = entidad.idDetalle,
                idPedido = entidad.idPedido,
                idProducto = entidad.idProducto,
                cantidad = entidad.cantidad,
                precioUnitario = entidad.precioUnitario
            };
        }

        public static List<DetallePedidoRespuestaDTO> ToRespuestaDTO(this IEnumerable<DetallePedidos> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
