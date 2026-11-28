using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>UsuariosMapper: cada campo llega a su destino, sin perder ni cruzar datos.</summary>
public class UsuariosMapperTests
{
    private static Usuarios Entidad() => new() { idUsuario = 7, nombres = "Laura", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "300", estado = true, idRol = 2, email = "laura@elcuate.com" };

    [Fact]
    public void ToRespuestaDTO_CopiaCadaCampoDeLaEntidad()
    {
        var dto = Entidad().ToRespuestaDTO();

        dto.idUsuario.Should().Be(7);
        dto.nombres.Should().Be("Laura");
        dto.apellidos.Should().Be("Gómez");
        dto.documento.Should().Be("1020");
        dto.idTipoDocumento.Should().Be(1);
        dto.telefono.Should().Be("300");
        dto.estado.Should().Be(true);
        dto.idRol.Should().Be(2);
        dto.email.Should().Be("laura@elcuate.com");
    }

    [Fact]
    public void ToRespuestaDTO_Lista_ConvierteCadaElementoEnOrden()
    {
        var lista = new List<Usuarios> { Entidad(), Entidad() };

        var dtos = lista.ToRespuestaDTO();

        dtos.Should().HaveCount(2).And.AllBeOfType<UsuarioRespuestaDTO>();
    }

    [Fact]
    public void ToRespuestaDTO_ListaVacia_DevuelveListaVacia()
    {
        new List<Usuarios>().ToRespuestaDTO().Should().BeEmpty();
    }
}
