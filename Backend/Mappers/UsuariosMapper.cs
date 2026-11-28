using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class UsuariosMapper
    {
        /// <summary>Conserva tildes y eñes legibles en la auditoría (escapa solo lo peligroso para HTML).</summary>
        private static readonly JsonSerializerOptions opcionesAuditoria = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

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

        public static UsuarioConAccionesDTO ToConAccionesDTO(this Usuarios entidad, AccionesUsuarioDTO acciones)
        {
            return new UsuarioConAccionesDTO
            {
                idUsuario = entidad.idUsuario,
                nombres = entidad.nombres,
                apellidos = entidad.apellidos,
                documento = entidad.documento,
                idTipoDocumento = entidad.idTipoDocumento,
                telefono = entidad.telefono,
                estado = entidad.estado,
                idRol = entidad.idRol,
                email = entidad.email,
                acciones = acciones
            };
        }

        /// <summary>
        /// Datos del usuario para Auditorias.datosAnteriores/datosNuevos. Nunca incluye el hash ni
        /// contraseñas. En una alta el id aún no existe y se omite (el correo, único, lo identifica).
        /// </summary>
        public static string ToAuditoria(this Usuarios u)
        {
            var datos = new Dictionary<string, object?>();
            if (u.idUsuario > 0) datos["idUsuario"] = u.idUsuario;
            datos["nombres"] = u.nombres;
            datos["apellidos"] = u.apellidos;
            datos["documento"] = u.documento;
            datos["idTipoDocumento"] = u.idTipoDocumento;
            datos["telefono"] = u.telefono;
            datos["estado"] = u.estado;
            datos["idRol"] = u.idRol;
            datos["email"] = u.email;

            return JsonSerializer.Serialize(datos, opcionesAuditoria);
        }

        /// <summary>Identificación mínima más un detalle, para cambios de contraseña (sin datos sensibles).</summary>
        public static string ToAuditoriaPassword(this Usuarios u, string detalle)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["idUsuario"] = u.idUsuario,
                ["nombres"] = u.nombres,
                ["apellidos"] = u.apellidos,
                ["email"] = u.email,
                ["detalle"] = detalle
            }, opcionesAuditoria);
        }
    }
}
