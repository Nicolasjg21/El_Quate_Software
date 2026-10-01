using ElQuateDePatty.DTOs.Kardex;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class KardexMapper
    {
        public static Kardex ToEntity(this KardexCrearDTO dto)
        {
            return new Kardex
            {
                idProducto = dto.idProducto,
                tipoMovimiento = dto.tipoMovimiento,
                cantidad = dto.cantidad,
                stockAnterior = dto.stockAnterior,
                stockNuevo = dto.stockNuevo,
                motivo = dto.motivo,
                fecha = dto.fecha
            };
        }

        public static KardexRespuestaDTO ToRespuestaDTO(this Kardex entidad)
        {
            return new KardexRespuestaDTO
            {
                idMovimiento = entidad.idMovimiento,
                idProducto = entidad.idProducto,
                tipoMovimiento = entidad.tipoMovimiento,
                cantidad = entidad.cantidad,
                stockAnterior = entidad.stockAnterior,
                stockNuevo = entidad.stockNuevo,
                motivo = entidad.motivo,
                fecha = entidad.fecha,
                idUsuario = entidad.idUsuario
            };
        }

        public static List<KardexRespuestaDTO> ToRespuestaDTO(this IEnumerable<Kardex> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
