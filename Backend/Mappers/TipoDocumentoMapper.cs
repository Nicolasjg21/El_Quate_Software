using ElQuateDePatty.DTOs.TipoDocumento;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class TipoDocumentoMapper
    {
        public static TipoDocumento ToEntity(this TipoDocumentoCrearDTO dto)
        {
            return new TipoDocumento
            {
                nombreTipo = dto.nombreTipo
            };
        }

        public static TipoDocumentoRespuestaDTO ToRespuestaDTO(this TipoDocumento entidad)
        {
            return new TipoDocumentoRespuestaDTO
            {
                idTipoDocumento = entidad.idTipoDocumento,
                nombreTipo = entidad.nombreTipo
            };
        }

        public static List<TipoDocumentoRespuestaDTO> ToRespuestaDTO(this IEnumerable<TipoDocumento> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
