namespace ElQuateDePatty.Services
{
    /// <summary>
    /// Impuesto Nacional al Consumo (INC) de bares y restaurantes: 8 % incluido en el precio de carta.
    /// El total cobrado ya trae el impuesto; aquí solo se separa en base gravable e impuesto.
    /// <list type="bullet">
    /// <item>base = total / 1,08, redondeada a pesos enteros (mitades hacia arriba, igual que Math.round del frontend).</item>
    /// <item>impuesto = total − base, de modo que base + impuesto siempre suma exactamente el total.</item>
    /// </list>
    /// </summary>
    public static class ImpuestoConsumo
    {
        /// <summary>Tarifa del INC: 8 %.</summary>
        public const decimal Tasa = 0.08m;

        /// <summary>Separa un total con el INC incluido en su base gravable y el impuesto.</summary>
        /// <param name="total">Total cobrado al cliente, en pesos, con el INC incluido.</param>
        public static (decimal baseGravable, decimal impuesto) Desglosar(decimal total)
        {
            var baseGravable = Math.Round(total / (1 + Tasa), 0, MidpointRounding.AwayFromZero);
            return (baseGravable, total - baseGravable);
        }
    }
}
