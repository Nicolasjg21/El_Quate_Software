using System.ComponentModel.DataAnnotations;
using ElQuateDePatty.DTOs.Proveedores;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>CB-06 · Validación del DTO de proveedor (partición de equivalencia y valores límite).</summary>
public class ProveedorCrearDTOTests
{
    private static bool EsValido(ProveedorCrearDTO dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

    [Theory]
    [InlineData("", false)]                         // vacío: [Required] lo rechaza
    [InlineData("   ", false)]                      // solo espacios: [Required] también lo rechaza
    [InlineData("3001234567", true)]                // caso feliz
    [InlineData("12345678901234567890", true)]      // 20 caracteres: justo en el límite
    [InlineData("123456789012345678901", false)]    // 21 caracteres: se pasa por uno
    public void Telefono_SegunLongitud_ValidaCorrectamente(string telefono, bool esperado)
    {
        // Arrange
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = telefono, direccion = "Cra 1" };

        // Act + Assert
        EsValido(dto).Should().Be(esperado);
    }
}
