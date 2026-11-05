using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Usuarios;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CambiarPasswordDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CambiarPasswordDTOValidacionTests
{
    private static CambiarPasswordDTO Valido() => new() { passwordActual = "ClaveVieja1", nuevaPassword = "ClaveNueva1", confirmarPassword = "ClaveNueva1" };

    [Fact]
    public void CambiarPasswordDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void passwordActual_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.passwordActual = "";

        Validar.Errores(dto).Should().Contain("passwordActual");
    }

    [Fact]
    public void passwordActual_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.passwordActual = "   ";

        Validar.Errores(dto).Should().Contain("passwordActual");
    }

    [Fact]
    public void passwordActual_128Caracteres_EsValido()
    {
        var dto = Valido();
        dto.passwordActual = new string('a', 128);

        Validar.Errores(dto).Should().NotContain("passwordActual");
    }

    [Fact]
    public void passwordActual_129Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.passwordActual = new string('a', 129);

        Validar.Errores(dto).Should().Contain("passwordActual");
    }

    [Fact]
    public void nuevaPassword_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nuevaPassword = "";

        Validar.Errores(dto).Should().Contain("nuevaPassword");
    }

    [Fact]
    public void nuevaPassword_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nuevaPassword = "   ";

        Validar.Errores(dto).Should().Contain("nuevaPassword");
    }

    [Fact]
    public void nuevaPassword_7Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nuevaPassword = new string('a', 7);

        Validar.Errores(dto).Should().Contain("nuevaPassword");
    }

    [Fact]
    public void nuevaPassword_8Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nuevaPassword = new string('a', 8);

        Validar.Errores(dto).Should().NotContain("nuevaPassword");
    }

    [Fact]
    public void nuevaPassword_128Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nuevaPassword = new string('a', 128);

        Validar.Errores(dto).Should().NotContain("nuevaPassword");
    }

    [Fact]
    public void nuevaPassword_129Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nuevaPassword = new string('a', 129);

        Validar.Errores(dto).Should().Contain("nuevaPassword");
    }

    [Fact]
    public void confirmarPassword_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.confirmarPassword = "";

        Validar.Errores(dto).Should().Contain("confirmarPassword");
    }

    [Fact]
    public void confirmarPassword_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.confirmarPassword = "   ";

        Validar.Errores(dto).Should().Contain("confirmarPassword");
    }

    [Fact]
    public void confirmarPassword_128Caracteres_EsValido()
    {
        var dto = Valido();
        dto.confirmarPassword = new string('a', 128);

        Validar.Errores(dto).Should().NotContain("confirmarPassword");
    }

    [Fact]
    public void confirmarPassword_129Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.confirmarPassword = new string('a', 129);

        Validar.Errores(dto).Should().Contain("confirmarPassword");
    }
}
