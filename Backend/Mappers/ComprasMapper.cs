using ElQuateDePatty.DTOs.Compras;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class ComprasMapper
    {
        public static Compras ToEntity(this CompraCrearDTO dto)
        {
            return new Compras
            {
                idProveedor = dto.idProveedor,
                fecha = dto.fecha,
                total = dto.total
            };
        }

        public static CompraRespuestaDTO ToRespuestaDTO(this Compras entidad)
        {
            return new CompraRespuestaDTO
            {
                idCompra = entidad.idCompra,
                idProveedor = entidad.idProveedor,
                fecha = entidad.fecha,
                total = entidad.total
            };
        }

        public static List<CompraRespuestaDTO> ToRespuestaDTO(this IEnumerable<Compras> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
