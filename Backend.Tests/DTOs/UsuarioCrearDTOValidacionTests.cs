using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Usuarios;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de UsuarioCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class UsuarioCrearDTOValidacionTests
{
    private static UsuarioCrearDTO Valido() => new() { nombres = "Laura", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "3001234567", estado = true, idRol = 1, email = "laura@elcuate.com", password = "ClaveSegura1" };

    [Fact]
    public void UsuarioCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombres_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombres = "";

        Validar.Errores(dto).Should().Contain("nombres");
    }

    [Fact]
    public void nombres_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombres = "   ";

        Validar.Errores(dto).Should().Contain("nombres");
    }

    [Fact]
    public void nombres_200Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombres = new string('a', 200);

        Validar.Errores(dto).Should().NotContain("nombres");
    }

    [Fact]
    public void nombres_201Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombres = new string('a', 201);

        Validar.Errores(dto).Should().Contain("nombres");
    }

    [Fact]
    public void apellidos_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.apellidos = "";

        Validar.Errores(dto).Should().Contain("apellidos");
    }

    [Fact]
    public void apellidos_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.apellidos = "   ";

        Validar.Errores(dto).Should().Contain("apellidos");
    }

    [Fact]
    public void apellidos_200Caracteres_EsValido()
    {
        var dto = Valido();
        dto.apellidos = new string('a', 200);

        Validar.Errores(dto).Should().NotContain("apellidos");
    }

    [Fact]
    public void apellidos_201Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.apellidos = new string('a', 201);

        Validar.Errores(dto).Should().Contain("apellidos");
    }

    [Fact]
    public void documento_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.documento = "";

        Validar.Errores(dto).Should().Contain("documento");
    }

    [Fact]
    public void documento_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.documento = "   ";

        Validar.Errores(dto).Should().Contain("documento");
    }

    [Fact]
    public void documento_50Caracteres_EsValido()
    {
        var dto = Valido();
        dto.documento = new string('a', 50);

        Validar.Errores(dto).Should().NotContain("documento");
    }

    [Fact]
    public void documento_51Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.documento = new string('a', 51);

        Validar.Errores(dto).Should().Contain("documento");
    }

    [Fact]
    public void idTipoDocumento_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idTipoDocumento = 1;

        Validar.Errores(dto).Should().NotContain("idTipoDocumento");
    }

    [Fact]
    public void idTipoDocumento_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idTipoDocumento = 0;

        Validar.Errores(dto).Should().Contain("idTipoDocumento");
    }

    [Fact]
    public void telefono_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = "";

        Validar.Errores(dto).Should().Contain("telefono");
    }

    [Fact]
    public void telefono_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = "   ";

        Validar.Errores(dto).Should().Contain("telefono");
    }

    [Fact]
    public void telefono_20Caracteres_EsValido()
    {
        var dto = Valido();
        dto.telefono = new string('a', 20);

        Validar.Errores(dto).Should().NotContain("telefono");
    }

    [Fact]
    public void telefono_21Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = new string('a', 21);

        Validar.Errores(dto).Should().Contain("telefono");
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
    public void password_7Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.password = new string('a', 7);

        Validar.Errores(dto).Should().Contain("password");
    }

    [Fact]
    public void password_8Caracteres_EsValido()
    {
        var dto = Valido();
        dto.password = new string('a', 8);

        Validar.Errores(dto).Should().NotContain("password");
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

    [Fact]
    public void idRol_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idRol = 1;

        Validar.Errores(dto).Should().NotContain("idRol");
    }

    [Fact]
    public void idRol_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idRol = 0;

        Validar.Errores(dto).Should().Contain("idRol");
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
}
