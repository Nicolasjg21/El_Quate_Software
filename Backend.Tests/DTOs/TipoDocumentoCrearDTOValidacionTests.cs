using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.TipoDocumento;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de TipoDocumentoCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class TipoDocumentoCrearDTOValidacionTests
{
    private static TipoDocumentoCrearDTO Valido() => new() { nombreTipo = "Cédula" };

    [Fact]
    public void TipoDocumentoCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreTipo_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreTipo = "";

        Validar.Errores(dto).Should().Contain("nombreTipo");
    }

    [Fact]
    public void nombreTipo_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreTipo = "   ";

        Validar.Errores(dto).Should().Contain("nombreTipo");
    }

    [Fact]
    public void nombreTipo_100Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreTipo = new string('a', 100);

        Validar.Errores(dto).Should().NotContain("nombreTipo");
    }

    [Fact]
    public void nombreTipo_101Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreTipo = new string('a', 101);

        Validar.Errores(dto).Should().Contain("nombreTipo");
    }
}
