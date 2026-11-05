using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Roles;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de RolCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class RolCrearDTOValidacionTests
{
    private static RolCrearDTO Valido() => new() { nombreRol = "Mesero" };

    [Fact]
    public void RolCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreRol_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreRol = "";

        Validar.Errores(dto).Should().Contain("nombreRol");
    }

    [Fact]
    public void nombreRol_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreRol = "   ";

        Validar.Errores(dto).Should().Contain("nombreRol");
    }

    [Fact]
    public void nombreRol_50Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreRol = new string('a', 50);

        Validar.Errores(dto).Should().NotContain("nombreRol");
    }

    [Fact]
    public void nombreRol_51Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreRol = new string('a', 51);

        Validar.Errores(dto).Should().Contain("nombreRol");
    }
}
