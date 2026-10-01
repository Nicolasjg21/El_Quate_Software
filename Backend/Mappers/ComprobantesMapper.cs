using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class ComprobantesMapper
    {
        public static Comprobantes ToEntity(this ComprobanteCrearDTO dto)
        {
            return new Comprobantes
            {
                idCuenta = dto.idCuenta,
                fecha = dto.fecha,
                total = dto.total,
                idMetodo = dto.idMetodo
            };
        }

        public static ComprobanteRespuestaDTO ToRespuestaDTO(this Comprobantes entidad)
        {
            return new ComprobanteRespuestaDTO
            {
                idComprobante = entidad.idComprobante,
                idCuenta = entidad.idCuenta,
                fecha = entidad.fecha,
                total = entidad.total,
                idMetodo = entidad.idMetodo
            };
        }

        public static List<ComprobanteRespuestaDTO> ToRespuestaDTO(this IEnumerable<Comprobantes> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
