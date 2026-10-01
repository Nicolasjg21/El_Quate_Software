using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Models;

namespace ElQuateDePatty.Mappers
{
    public static class CuentasMapper
    {
        public static Cuentas ToEntity(this CuentaCrearDTO dto)
        {
            return new Cuentas
            {
                idMesa = dto.idMesa,
                estado = dto.estado,
                fechaApertura = dto.fechaApertura,
                fechaCierre = dto.fechaCierre,
                total = dto.total
            };
        }

        public static CuentaRespuestaDTO ToRespuestaDTO(this Cuentas entidad)
        {
            return new CuentaRespuestaDTO
            {
                idCuenta = entidad.idCuenta,
                idMesa = entidad.idMesa,
                estado = entidad.estado,
                fechaApertura = entidad.fechaApertura,
                fechaCierre = entidad.fechaCierre,
                total = entidad.total
            };
        }

        public static List<CuentaRespuestaDTO> ToRespuestaDTO(this IEnumerable<Cuentas> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
