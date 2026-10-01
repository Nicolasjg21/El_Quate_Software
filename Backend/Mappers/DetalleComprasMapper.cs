using ElQuateDePatty.DTOs.DetalleCompras;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class DetalleComprasMapper
    {
        public static DetalleCompras ToEntity(this DetalleCompraCrearDTO dto)
        {
            return new DetalleCompras
            {
                idCompra = dto.idCompra,
                idProducto = dto.idProducto,
                cantidad = dto.cantidad,
                precioCompra = dto.precioCompra
            };
        }

        public static DetalleCompraRespuestaDTO ToRespuestaDTO(this DetalleCompras entidad)
        {
            return new DetalleCompraRespuestaDTO
            {
                idDetalleCompra = entidad.idDetalleCompra,
                idCompra = entidad.idCompra,
                idProducto = entidad.idProducto,
                cantidad = entidad.cantidad,
                precioCompra = entidad.precioCompra
            };
        }

        public static List<DetalleCompraRespuestaDTO> ToRespuestaDTO(this IEnumerable<DetalleCompras> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
