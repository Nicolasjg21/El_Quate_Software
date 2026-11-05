using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Mesas;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de MesaCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class MesaCrearDTOValidacionTests
{
    private static MesaCrearDTO Valido() => new() { numeroMesa = 3, estado = "Libre" };

    [Fact]
    public void MesaCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void numeroMesa_Igual0_EsValido()
    {
        var dto = Valido();
        dto.numeroMesa = 0;

        Validar.Errores(dto).Should().NotContain("numeroMesa");
    }

    [Fact]
    public void numeroMesa_IgualMenos1_EsInvalido()
    {
        var dto = Valido();
        dto.numeroMesa = -1;

        Validar.Errores(dto).Should().Contain("numeroMesa");
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

    [Fact]
    public void estado_20Caracteres_EsValido()
    {
        var dto = Valido();
        dto.estado = new string('a', 20);

        Validar.Errores(dto).Should().NotContain("estado");
    }

    [Fact]
    public void estado_21Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.estado = new string('a', 21);

        Validar.Errores(dto).Should().Contain("estado");
    }
}
