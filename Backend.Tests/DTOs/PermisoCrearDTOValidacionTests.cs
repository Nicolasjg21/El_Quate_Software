using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Permisos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de PermisoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class PermisoCrearDTOValidacionTests
{
    private static PermisoCrearDTO Valido() => new() { nombrePermiso = "ventas.cobrar" };

    [Fact]
    public void PermisoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombrePermiso_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombrePermiso = "";

        Validar.Errores(dto).Should().Contain("nombrePermiso");
    }

    [Fact]
    public void nombrePermiso_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombrePermiso = "   ";

        Validar.Errores(dto).Should().Contain("nombrePermiso");
    }

    [Fact]
    public void nombrePermiso_100Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombrePermiso = new string('a', 100);

        Validar.Errores(dto).Should().NotContain("nombrePermiso");
    }

    [Fact]
    public void nombrePermiso_101Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombrePermiso = new string('a', 101);

        Validar.Errores(dto).Should().Contain("nombrePermiso");
    }
}
