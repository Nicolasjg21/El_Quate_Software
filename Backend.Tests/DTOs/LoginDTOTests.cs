using System.ComponentModel.DataAnnotations;
using ElQuateDePatty.DTOs.Usuarios;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>Validación del DTO de inicio de sesión (partición de equivalencia).</summary>
public class LoginDTOTests
{
    private static bool EsValido(LoginDTO dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

    [Theory]
    [InlineData("admin@elcuate.com", "Clave123", true)]   // partición válida
    [InlineData("", "Clave123", false)]                   // correo vacío
    [InlineData("abc", "Clave123", false)]                // correo mal formado
    [InlineData("admin@elcuate.com", "", false)]          // contraseña vacía
    public void Login_SegunDatos_ValidaCorrectamente(string email, string password, bool esperado)
    {
        EsValido(new LoginDTO { email = email, password = password }).Should().Be(esperado);
    }
}
