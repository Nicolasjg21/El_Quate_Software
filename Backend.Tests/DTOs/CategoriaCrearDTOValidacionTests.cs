using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Categorias;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CategoriaCrearDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CategoriaCrearDTOValidacionTests
{
    private static CategoriaCrearDTO Valido() => new() { nombreCategoria = "Cervezas" };

    [Fact]
    public void CategoriaCrearDTO_DatosCompletos_EsValido()
    {
        Validar.Errores(Valido()).Should().BeEmpty();
    }

    [Fact]
    public void nombreCategoria_Vacio_EsInvalido()
    {
        var dto = Valido();
        dto.nombreCategoria = "";

        Validar.Errores(dto).Should().Contain("nombreCategoria");
    }

    [Fact]
    public void nombreCategoria_SoloEspacios_EsInvalido()
    {
        var dto = Valido();
        dto.nombreCategoria = "   ";

        Validar.Errores(dto).Should().Contain("nombreCategoria");
    }

    [Fact]
    public void nombreCategoria_100Caracteres_EsValido()
    {
        var dto = Valido();
        dto.nombreCategoria = new string('a', 100);

        Validar.Errores(dto).Should().NotContain("nombreCategoria");
    }

    [Fact]
    public void nombreCategoria_101Caracteres_EsInvalido()
    {
        var dto = Valido();
        dto.nombreCategoria = new string('a', 101);

        Validar.Errores(dto).Should().Contain("nombreCategoria");
    }
}
