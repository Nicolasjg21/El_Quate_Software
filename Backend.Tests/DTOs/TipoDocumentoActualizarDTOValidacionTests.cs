using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.TipoDocumento;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de TipoDocumentoActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class TipoDocumentoActualizarDTOValidacionTests
{
    private static TipoDocumentoActualizarDTO Valido() => new() { idTipoDocumento = 1, nombreTipo = "Cédula" };

    [Fact]
    public void TipoDocumentoActualizarDTO_DatosCompletos_EsValido()
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
}
