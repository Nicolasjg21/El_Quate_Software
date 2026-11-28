using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Kardex;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de KardexActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class KardexActualizarDTOValidacionTests
{
    private static KardexActualizarDTO Valido() => new() { idMovimiento = 1, idUsuario = 1, idProducto = 1, tipoMovimiento = "ENTRADA", cantidad = 10, fecha = new DateTime(2026, 11, 5) };

    [Fact]
    public void KardexActualizarDTO_DatosCompletos_EsValido()
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
}
