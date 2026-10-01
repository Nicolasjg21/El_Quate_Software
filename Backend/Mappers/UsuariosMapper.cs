using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class UsuariosMapper
    {
        public static UsuarioRespuestaDTO ToRespuestaDTO(this Usuarios entidad)
        {
            return new UsuarioRespuestaDTO
            {
                idUsuario = entidad.idUsuario,
                nombres = entidad.nombres,
                apellidos = entidad.apellidos,
                documento = entidad.documento,
                idTipoDocumento = entidad.idTipoDocumento,
                telefono = entidad.telefono,
                estado = entidad.estado,
                idRol = entidad.idRol,
                email = entidad.email
            };
        }

        public static List<UsuarioRespuestaDTO> ToRespuestaDTO(this IEnumerable<Usuarios> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
