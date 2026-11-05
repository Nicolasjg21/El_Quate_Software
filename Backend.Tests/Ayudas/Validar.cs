using System.ComponentModel.DataAnnotations;

namespace Backend.Tests.Ayudas;

/// <summary>
/// Ejecuta las DataAnnotations de un DTO igual que lo hace ASP.NET antes de llegar al controlador,
/// y devuelve los nombres de los campos que fallaron.
/// </summary>
public static class Validar
{
    public static List<string> Errores(object dto)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);
        return resultados.SelectMany(r => r.MemberNames).ToList();
    }
}
