using ElQuateDePatty.Models;

namespace ElQuateDePatty.Services
{
    /// <summary>Cómo terminó un intento de cobro.</summary>
    public enum EstadoCobro
    {
        /// <summary>Se emitió el comprobante, se cerró la cuenta, se pagaron los pedidos y se liberó la mesa.</summary>
        Cobrada,

        /// <summary>La cuenta no existe.</summary>
        CuentaNoExiste,

        /// <summary>La cuenta ya tiene comprobante: no se cobra dos veces.</summary>
        YaCobrada,

        /// <summary>La cuenta no está abierta y no tiene comprobante (estado inconsistente).</summary>
        CuentaNoAbierta,

        /// <summary>El método de pago no existe.</summary>
        MetodoNoExiste,

        /// <summary>La cuenta no tiene productos.</summary>
        SinProductos,

        /// <summary>El total de la cuenta no coincide con el que vio quien cobra.</summary>
        TotalDesactualizado
    }

    /// <summary>
    /// Resultado de <c>ICuentasRepository.Cobrar</c>.
    /// <para>comprobante: el emitido (Cobrada) o el que ya existía (YaCobrada).</para>
    /// <para>totalActual: total calculado por el servidor con las líneas de la cuenta.</para>
    /// </summary>
    public sealed record ResultadoCobro(
        EstadoCobro estado,
        Cuentas? cuenta = null,
        Comprobantes? comprobante = null,
        decimal totalActual = 0m);
}
