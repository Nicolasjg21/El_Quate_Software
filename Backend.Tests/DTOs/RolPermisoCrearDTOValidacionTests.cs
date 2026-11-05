using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.RolesPermisos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de RolPermisoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class RolPermisoCrearDTOValidacionTests
{
    private static RolPermisoCrearDTO Valido() => new() { idRol = 1, idPermiso = 1 };

    [Fact]
    public void RolPermisoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
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
    public void idPermiso_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idPermiso = 1;

        Validar.Errores(dto).Should().NotContain("idPermiso");
    }

    [Fact]
    public void idPermiso_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idPermiso = 0;

        Validar.Errores(dto).Should().Contain("idPermiso");
    }
}
