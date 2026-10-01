using ElQuateDePatty.DTOs.MetodosPago;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class MetodosPagoMapper
    {
        public static MetodosPago ToEntity(this MetodoPagoCrearDTO dto)
        {
            return new MetodosPago
            {
                nombreMetodo = dto.nombreMetodo
            };
        }

        public static MetodoPagoRespuestaDTO ToRespuestaDTO(this MetodosPago entidad)
        {
            return new MetodoPagoRespuestaDTO
            {
                idMetodo = entidad.idMetodo,
                nombreMetodo = entidad.nombreMetodo
            };
        }

        public static List<MetodoPagoRespuestaDTO> ToRespuestaDTO(this IEnumerable<MetodosPago> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
