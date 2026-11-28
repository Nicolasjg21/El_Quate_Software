using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Permisos;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de PermisoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class PermisoActualizarDTOValidacionTests
{
    private static PermisoActualizarDTO Valido() => new() { idPermiso = 1, nombrePermiso = "ventas.cobrar" };

    [Fact]
    public void PermisoActualizarDTO_DatosCompletos_EsValido()
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
}
