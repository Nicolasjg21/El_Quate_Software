using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Proveedores;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de ProveedorCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class ProveedorCrearDTOValidacionTests
{
    private static ProveedorCrearDTO Valido() => new() { nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

    [Fact]
    public void ProveedorCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreProveedor_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProveedor = "";

        Validar.Errores(dto).Should().Contain("nombreProveedor");
    }

    [Fact]
    public void nombreProveedor_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProveedor = "   ";

        Validar.Errores(dto).Should().Contain("nombreProveedor");
    }

    [Fact]
    public void nombreProveedor_200Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreProveedor = new string('a', 200);

        Validar.Errores(dto).Should().NotContain("nombreProveedor");
    }

    [Fact]
    public void nombreProveedor_201Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreProveedor = new string('a', 201);

        Validar.Errores(dto).Should().Contain("nombreProveedor");
    }

    [Fact]
    public void telefono_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = "";

        Validar.Errores(dto).Should().Contain("telefono");
    }

    [Fact]
    public void telefono_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.telefono = "   ";

        Validar.Errores(dto).Should().Contain("telefono");
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

    [Fact]
    public void direccion_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.direccion = "";

        Validar.Errores(dto).Should().Contain("direccion");
    }

    [Fact]
    public void direccion_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.direccion = "   ";

        Validar.Errores(dto).Should().Contain("direccion");
    }

    [Fact]
    public void direccion_150Caracteres_EsValido()
    {
        var dto = Valido();
        dto.direccion = new string('a', 150);

        Validar.Errores(dto).Should().NotContain("direccion");
    }

    [Fact]
    public void direccion_151Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.direccion = new string('a', 151);

        Validar.Errores(dto).Should().Contain("direccion");
    }
}
