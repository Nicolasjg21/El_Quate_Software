using ElQuateDePatty.DTOs.Comprobantes;
using ElQuateDePatty.Models;
using ElQuateDePatty.Services;

namespace ElQuateDePatty.Mappers
{
    public static class ComprobantesMapper
    {
        /// <summary>
        /// Comprobante a partir del DTO. La base gravable y el INC se calculan aquí desde el total,
        /// nunca se reciben del cliente.
        /// </summary>
        public static Comprobantes ToEntity(this ComprobanteCrearDTO dto)
        {
            var comprobante = new Comprobantes
            {
                idCuenta = dto.idCuenta,
                fecha = dto.fecha,
                total = dto.total,
                idMetodo = dto.idMetodo
            };
            comprobante.AplicarImpuesto();
            return comprobante;
        }

        /// <summary>Calcula baseGravable, impuesto y tasaImpuesto a partir del total (INC 8 % incluido).</summary>
        public static void AplicarImpuesto(this Comprobantes comprobante)
        {
            (comprobante.baseGravable, comprobante.impuesto) = ImpuestoConsumo.Desglosar(comprobante.total);
            comprobante.tasaImpuesto = ImpuestoConsumo.Tasa;
        }

        public static ComprobanteRespuestaDTO ToRespuestaDTO(this Comprobantes entidad)
        {
            return new ComprobanteRespuestaDTO
            {
                idComprobante = entidad.idComprobante,
                idCuenta = entidad.idCuenta,
                fecha = entidad.fecha,
                total = entidad.total,
                idMetodo = entidad.idMetodo,
                baseGravable = entidad.baseGravable,
                impuesto = entidad.impuesto,
                tasaImpuesto = entidad.tasaImpuesto
            };
        }

        public static List<ComprobanteRespuestaDTO> ToRespuestaDTO(this IEnumerable<Comprobantes> entidades)
        {
            return entidades.Select(entidad => entidad.ToRespuestaDTO()).ToList();
        }
    }
}
