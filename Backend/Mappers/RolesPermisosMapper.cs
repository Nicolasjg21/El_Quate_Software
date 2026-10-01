using ElQuateDePatty.DTOs.RolesPermisos;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class RolesPermisosMapper
    {
        public static RolesPermisos ToEntity(this RolPermisoCrearDTO dto)
        {
            return new RolesPermisos
            {
                idRol = dto.idRol,
                idPermiso = dto.idPermiso
            };
        }

        public static RolPermisoRespuestaDTO ToRespuestaDTO(this RolesPermisos entidad)
        {
            return new RolPermisoRespuestaDTO
            {
                idRol = entidad.idRol,
                idPermiso = entidad.idPermiso
            };
        }

        public static List<RolPermisoRespuestaDTO> ToRespuestaDTO(this IEnumerable<RolesPermisos> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
