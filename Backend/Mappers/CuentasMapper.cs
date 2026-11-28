using ElQuateDePatty.DTOs.Cuentas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Services;

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

        /// <summary>Respuesta de un cobro (emitido o ya existente). Requiere cuenta y comprobante.</summary>
        public static CobroRespuestaDTO ToCobroRespuestaDTO(this ResultadoCobro resultado)
        {
            var comprobante = resultado.comprobante
                ?? throw new InvalidOperationException("El resultado del cobro no tiene comprobante.");
            var cuenta = resultado.cuenta
                ?? throw new InvalidOperationException("El resultado del cobro no tiene cuenta.");

            return new CobroRespuestaDTO
            {
                idComprobante = comprobante.idComprobante,
                idCuenta = cuenta.idCuenta,
                numeroMesa = cuenta.mesa?.numeroMesa ?? 0,
                idMetodo = comprobante.idMetodo,
                fecha = comprobante.fecha,
                fechaApertura = cuenta.fechaApertura,
                fechaCierre = cuenta.fechaCierre,
                total = comprobante.total,
                baseGravable = comprobante.baseGravable,
                impuesto = comprobante.impuesto,
                tasaImpuesto = comprobante.tasaImpuesto
            };
        }
    }
}
