using System.ComponentModel.DataAnnotations;

namespace ElQuateDePatty.DTOs.Validaciones
{
    /// <summary>
    /// Valida que un importe en pesos colombianos (COP) no tenga centavos (RN-04).
    /// Los valores nulos se consideran válidos; la obligatoriedad se valida con <see cref="RequiredAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class PesosEnterosAttribute : ValidationAttribute
    {
        /// <summary>Inicializa el atributo con el mensaje de error predeterminado.</summary>
        public PesosEnterosAttribute()
            : base("El campo {0} debe ser un valor en pesos (COP) sin centavos.")
        {
        }

        /// <inheritdoc />
        public override bool IsValid(object? value) => value switch
        {
            null => true,
            decimal importe => decimal.Truncate(importe) == importe,
            _ => false
        };
    }
}