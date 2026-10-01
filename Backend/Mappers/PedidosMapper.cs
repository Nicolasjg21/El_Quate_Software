using ElQuateDePatty.DTOs.Pedidos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class PedidosMapper
    {
        public static Pedidos ToEntity(this PedidoCrearDTO dto)
        {
            return new Pedidos
            {
                idCuenta = dto.idCuenta,
                idUsuario = dto.idUsuario,
                fecha = dto.fecha,
                estadoPedido = dto.estadoPedido
            };
        }

        public static PedidoRespuestaDTO ToRespuestaDTO(this Pedidos entidad)
        {
            return new PedidoRespuestaDTO
            {
                idPedido = entidad.idPedido,
                idCuenta = entidad.idCuenta,
                idUsuario = entidad.idUsuario,
                fecha = entidad.fecha,
                estadoPedido = entidad.estadoPedido
            };
        }

        public static List<PedidoRespuestaDTO> ToRespuestaDTO(this IEnumerable<Pedidos> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
