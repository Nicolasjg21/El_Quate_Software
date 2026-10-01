using ElQuateDePatty.DTOs.Roles;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class RolesMapper
    {
        public static Roles ToEntity(this RolCrearDTO dto)
        {
            return new Roles
            {
                nombreRol = dto.nombreRol
            };
        }

        public static RolRespuestaDTO ToRespuestaDTO(this Roles entidad)
        {
            return new RolRespuestaDTO
            {
                idRol = entidad.idRol,
                nombreRol = entidad.nombreRol
            };
        }

        public static List<RolRespuestaDTO> ToRespuestaDTO(this IEnumerable<Roles> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
