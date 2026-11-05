using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Auditorias;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de AuditoriaActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class AuditoriaActualizarDTOValidacionTests
{
    private static AuditoriaActualizarDTO Valido() => new() { idAuditoria = 1, idUsuario = 1, tabla = "Productos", accion = "UPDATE", fecha = new DateTime(2026, 11, 5) };

    [Fact]
    public void AuditoriaActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void idUsuario_Igual1_EsValido()
    {
        var dto = Valido();
        dto.idUsuario = 1;

        Validar.Errores(dto).Should().NotContain("idUsuario");
    }

    [Fact]
    public void idUsuario_Igual0_EsInvalido()
    {
        var dto = Valido();
        dto.idUsuario = 0;

        Validar.Errores(dto).Should().Contain("idUsuario");
    }

    [Fact]
    public void tabla_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.tabla = "";

        Validar.Errores(dto).Should().Contain("tabla");
    }

    [Fact]
    public void tabla_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.tabla = "   ";

        Validar.Errores(dto).Should().Contain("tabla");
    }
}
