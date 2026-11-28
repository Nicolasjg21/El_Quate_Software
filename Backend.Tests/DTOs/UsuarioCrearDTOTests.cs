using System.ComponentModel.DataAnnotations;
using ElQuateDePatty.DTOs.Usuarios;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>Validación del DTO de creación de usuario: valores límite y particiones.</summary>
public class UsuarioCrearDTOTests
{
    private static UsuarioCrearDTO Valido() => new()
    {
        nombres = "Laura", apellidos = "Gómez", documento = "1020304050", idTipoDocumento = 1,
        telefono = "3001234567", password = "ClaveSegura1", estado = true, idRol = 1, email = "laura@elcuate.com"
    };

    private static bool EsValido(UsuarioCrearDTO dto) =>
        Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

    [Fact]
    public void Usuario_DatosCompletos_EsValido()
    {
        EsValido(Valido()).Should().BeTrue();
    }

    // Contraseña: mínimo 8, máximo 128 (valores límite)
    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Password_SegunLongitud_ValidaCorrectamente(int largo, bool esperado)
    {
        var dto = Valido();
        dto.password = new string('a', largo);

        EsValido(dto).Should().Be(esperado);
    }

    // Correo: partición válida e inválidas
    [Theory]
    [InlineData("laura@elcuate.com", true)]
    [InlineData("laura", false)]
    [InlineData("", false)]
    public void Email_SegunFormato_ValidaCorrectamente(string email, bool esperado)
    {
        var dto = Valido();
        dto.email = email;

        EsValido(dto).Should().Be(esperado);
    }

    // Rol y tipo de documento deben ser >= 1
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void IdRol_SegunValor_ValidaCorrectamente(int idRol, bool esperado)
    {
        var dto = Valido();
        dto.idRol = idRol;

        EsValido(dto).Should().Be(esperado);
    }
}
