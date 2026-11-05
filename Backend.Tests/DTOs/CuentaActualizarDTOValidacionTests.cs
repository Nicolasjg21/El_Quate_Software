using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Cuentas;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CuentaActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CuentaActualizarDTOValidacionTests
{
    private static CuentaActualizarDTO Valido() => new() { idCuenta = 1, idMesa = 1, estado = "Abierta", fechaApertura = new DateTime(2026, 11, 5), total = 0m };

    [Fact]
    public void CuentaActualizarDTO_DatosCompletos_EsValido()
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
