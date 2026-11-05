using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Mappers;
using ElQuateDePatty.Models;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Mappers;

/// <summary>Los mappers copian los datos correctos y no exponen información sensible.</summary>
public class MappersTests
{
    [Fact]
    public void ProveedorToEntity_CopiaTodosLosCampos()
    {
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = "300", direccion = "Cra 1" };

        var entidad = dto.ToEntity();

        entidad.nombreProveedor.Should().Be("Bavaria");
        entidad.telefono.Should().Be("300");
        entidad.direccion.Should().Be("Cra 1");
        entidad.idProveedor.Should().Be(0);   // el id lo asigna la base, no el cliente
    }

    [Fact]
    public void ProveedoresToRespuestaDTO_Lista_ConviertePorElemento()
    {
        var lista = new List<Proveedores>
        {
            new() { idProveedor = 1, nombreProveedor = "Bavaria" },
            new() { idProveedor = 2, nombreProveedor = "Postobón" }
        };

        lista.ToRespuestaDTO().Select(p => p.idProveedor).Should().Equal(1, 2);
    }

    [Fact]
    public void UsuarioToRespuestaDTO_NoExponeElHashDeLaContrasena()
    {
        var usuario = new Usuarios { idUsuario = 1, nombres = "Laura", email = "laura@elcuate.com", passwordHash = "AQAAAAIAAYagAAAAE-secreto" };

        var dto = usuario.ToRespuestaDTO();

        dto.email.Should().Be("laura@elcuate.com");
        // Seguridad: la respuesta no tiene ninguna propiedad con la contraseña.
        dto.GetType().GetProperties().Select(p => p.Name.ToLower()).Should().NotContain(n => n.Contains("password"));
    }
}
