using ElQuateDePatty.DTOs.Auditorias;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class AuditoriasMapper
    {
        public static Auditorias ToEntity(this AuditoriaCrearDTO dto)
        {
            return new Auditorias
            {
                tabla = dto.tabla,
                accion = dto.accion,
                fecha = dto.fecha,
                datosAnteriores = dto.datosAnteriores,
                datosNuevos = dto.datosNuevos
            };
        }

        public static AuditoriaRespuestaDTO ToRespuestaDTO(this Auditorias entidad)
        {
            return new AuditoriaRespuestaDTO
            {
                idAuditoria = entidad.idAuditoria,
                tabla = entidad.tabla,
                accion = entidad.accion,
                idUsuario = entidad.idUsuario,
                fecha = entidad.fecha,
                datosAnteriores = entidad.datosAnteriores,
                datosNuevos = entidad.datosNuevos
            };
        }

        public static List<AuditoriaRespuestaDTO> ToRespuestaDTO(this IEnumerable<Auditorias> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
