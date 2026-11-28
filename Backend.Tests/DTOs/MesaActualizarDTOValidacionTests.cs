using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Mesas;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de MesaActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class MesaActualizarDTOValidacionTests
{
    private static MesaActualizarDTO Valido() => new() { idMesa = 1, numeroMesa = 3, estado = "Libre" };

    [Fact]
    public void MesaActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void estado_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.estado = "";

        Validar.Errores(dto).Should().Contain("estado");
    }

    [Fact]
    public void estado_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.estado = "   ";

        Validar.Errores(dto).Should().Contain("estado");
    }
}
