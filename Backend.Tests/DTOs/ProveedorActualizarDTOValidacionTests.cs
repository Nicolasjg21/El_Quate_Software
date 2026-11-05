using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Proveedores;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ProveedorActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ProveedorActualizarDTOValidacionTests
{
    private static ProveedorActualizarDTO Valido() => new() { idProveedor = 1, nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

    [Fact]
    public void ProveedorActualizarDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void telefono_20Caracteres_EsValido()
    {
        var dto = Valido();
        dto.telefono = new string('a', 20);

        Validar.Errores(dto).Should().NotContain("telefono");
    }

    [Fact]
    public void telefono_21Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = new string('a', 21);

        Validar.Errores(dto).Should().Contain("telefono");
    }
}
