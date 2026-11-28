namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Costo promedio ponderado móvil (método perpetuo) del inventario.
    /// - Una ENTRADA con costo de compra recalcula el promedio con lo que hay en bodega:
    ///     promedio = (stock anterior × promedio anterior + cantidad × costo de compra) / (stock anterior + cantidad)
    /// - Cualquier otro movimiento (venta, devolución a bodega, ajuste o entrada sin costo) se valora
    ///   al promedio vigente y no lo cambia.
    /// Los valores se redondean a 2 decimales (centavos), igual que las columnas de la base.
    /// </summary>
    public static class CostoPromedio
    {
        /// <param name="stockAnterior">Stock antes del movimiento.</param>
        /// <param name="promedioAnterior">Costo promedio vigente antes del movimiento (0 si el producto no tiene movimientos).</param>
        /// <param name="tipoMovimiento">ENTRADA, SALIDA o AJUSTE.</param>
        /// <param name="cantidad">Unidades del movimiento.</param>
        /// <param name="costoCompra">Costo unitario de compra; solo se usa en una ENTRADA.</param>
        /// <returns>Costo unitario con que se valora el movimiento y costo promedio resultante.</returns>
        public static (decimal costoUnitario, decimal costoPromedio) Calcular(
            int stockAnterior, decimal promedioAnterior, string tipoMovimiento, int cantidad, decimal? costoCompra)
        {
            if (tipoMovimiento == "ENTRADA" && costoCompra.HasValue && cantidad > 0)
            {
                var stockPrevio = Math.Max(stockAnterior, 0);
                var costo = costoCompra.Value;

                var promedio = stockPrevio == 0
                    ? costo
                    : (stockPrevio * promedioAnterior + cantidad * costo) / (stockPrevio + cantidad);

                return (Redondear(costo), Redondear(promedio));
            }

            return (promedioAnterior, promedioAnterior);
        }

        private static decimal Redondear(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }
    }
}
