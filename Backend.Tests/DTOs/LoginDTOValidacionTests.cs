using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Usuarios;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de LoginDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class LoginDTOValidacionTests
{
    private static LoginDTO Valido() => new() { email = "admin@elcuate.com", password = "Clave123" };

    [Fact]
    public void LoginDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void email_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.email = "";

        Validar.Errores(dto).Should().Contain("email");
    }

    [Fact]
    public void email_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.email = "   ";

        Validar.Errores(dto).Should().Contain("email");
    }

    [Fact]
    public void email_SinArroba_EsInvalido()
    {
        var dto = Valido();
        dto.email = "laura.elcuate.com";

        Validar.Errores(dto).Should().Contain("email");
    }

    [Fact]
    public void email_ConArroba_EsValido()
    {
        var dto = Valido();
        dto.email = "laura@elcuate.com";

        Validar.Errores(dto).Should().NotContain("email");
    }

    [Fact]
    public void email_De254Caracteres_EsValido()
    {
        var dto = Valido();
        dto.email = new string('a', 242) + "@elcuate.com";

        Validar.Errores(dto).Should().NotContain("email");
    }

    [Fact]
    public void email_De255Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.email = new string('a', 243) + "@elcuate.com";

        Validar.Errores(dto).Should().Contain("email");
    }

    [Fact]
    public void password_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.password = "";

        Validar.Errores(dto).Should().Contain("password");
    }

    [Fact]
    public void password_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.password = "   ";

        Validar.Errores(dto).Should().Contain("password");
    }

    [Fact]
    public void password_128Caracteres_EsValido()
    {
        var dto = Valido();
        dto.password = new string('a', 128);

        Validar.Errores(dto).Should().NotContain("password");
    }

    [Fact]
    public void password_129Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.password = new string('a', 129);

        Validar.Errores(dto).Should().Contain("password");
    }
}
