using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class ProveedoresMapper
    {
        public static Proveedores ToEntity(this ProveedorCrearDTO dto)
        {
            return new Proveedores
            {
                nombreProveedor = dto.nombreProveedor,
                telefono = dto.telefono,
                direccion = dto.direccion
            };
        }

        public static ProveedorRespuestaDTO ToRespuestaDTO(this Proveedores entidad)
        {
            return new ProveedorRespuestaDTO
            {
                idProveedor = entidad.idProveedor,
                nombreProveedor = entidad.nombreProveedor,
                telefono = entidad.telefono,
                direccion = entidad.direccion
            };
        }

        public static List<ProveedorRespuestaDTO> ToRespuestaDTO(this IEnumerable<Proveedores> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
