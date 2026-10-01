using ElQuateDePatty.DTOs.Permisos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class PermisosMapper
    {
        public static Permisos ToEntity(this PermisoCrearDTO dto)
        {
            return new Permisos
            {
                nombrePermiso = dto.nombrePermiso
            };
        }

        public static PermisoRespuestaDTO ToRespuestaDTO(this Permisos entidad)
        {
            return new PermisoRespuestaDTO
            {
                idPermiso = entidad.idPermiso,
                nombrePermiso = entidad.nombrePermiso
            };
        }

        public static List<PermisoRespuestaDTO> ToRespuestaDTO(this IEnumerable<Permisos> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
