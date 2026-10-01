using ElQuateDePatty.DTOs.Mesas;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class MesasMapper
    {
        public static Mesas ToEntity(this MesaCrearDTO dto)
        {
            return new Mesas
            {
                numeroMesa = dto.numeroMesa,
                estado = dto.estado
            };
        }

        public static MesaRespuestaDTO ToRespuestaDTO(this Mesas entidad)
        {
            return new MesaRespuestaDTO
            {
                idMesa = entidad.idMesa,
                numeroMesa = entidad.numeroMesa,
                estado = entidad.estado
            };
        }

        public static List<MesaRespuestaDTO> ToRespuestaDTO(this IEnumerable<Mesas> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
