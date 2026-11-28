using Backend.Tests.Ayudas;
using ElQuateDePatty.DTOs.Categorias;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.DTOs;

/// <summary>
/// Reglas de validación de CategoriaActualizarDTO: cada regla se prueba en su borde (valor límite)
/// y justo por fuera. Una prueba inválida verifica además que el error sea en ESE campo.
/// </summary>
public class CategoriaActualizarDTOValidacionTests
{
    private static CategoriaActualizarDTO Valido() => new() { idCategoria = 1, nombreCategoria = "Cervezas" };

    [Fact]
    public void CategoriaActualizarDTO_DatosCompletos_EsValido()
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
}
