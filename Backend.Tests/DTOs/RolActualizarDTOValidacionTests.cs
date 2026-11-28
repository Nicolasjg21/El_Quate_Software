using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Roles;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de RolActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class RolActualizarDTOValidacionTests
{
    private static RolActualizarDTO Valido() => new() { idRol = 1, nombreRol = "Mesero" };

    [Fact]
    public void RolActualizarDTO_DatosCompletos_EsValido()
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
}
