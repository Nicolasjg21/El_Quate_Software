using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Proveedores;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Pruebas unitarias (caja blanca) de ProveedoresController.
/// El repositorio se reemplaza por un doble de Moq; no se usa base de datos.
/// </summary>
public class ProveedoresControllerTests
{
    // xUnit crea una instancia nueva de esta clase por cada prueba: nadie comparte estado.
    private readonly Mock<IProveedoresRepository> repoMock = new();
    private readonly ProveedoresController sut;

    public ProveedoresControllerTests()
    {
        // NullLogger es un "dummy": el constructor lo exige, pero no hace nada.
        sut = new ProveedoresController(repoMock.Object, NullLogger<ProveedoresController>.Instance)
        {
            // El bloque catch del controller usa Request.Method: se le da un contexto HTTP falso.
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // CB-01 · Camino 1: decisión A verdadera (id <= 0)
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetProveedoresById_IdNoPositivo_Retorna400SinConsultarRepositorio(int id)
    {
        // Act
        var resultado = await sut.GetProveedoresById(id);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProveedoresById(It.IsAny<int>()), Times.Never);
    }

    // CB-02 · Camino 2: A falsa, B verdadera (no existe)
    [Fact]
    public async Task GetProveedoresById_NoExiste_Retorna404()
    {
        // Arrange
        repoMock.Setup(r => r.GetProveedoresById(99)).ReturnsAsync((Proveedores?)null);

        // Act
        var resultado = await sut.GetProveedoresById(99);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    // CB-03 · Camino 3: A falsa, B falsa (existe)
    [Fact]
    public async Task GetProveedoresById_Existe_Retorna200ConElProveedor()
    {
        // Arrange
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores
        {
            idProveedor = 1, nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1"
        });

        // Act
        var resultado = await sut.GetProveedoresById(1);

        // Assert: la API responde { statusCode, message, data }; "data" se lee con reflexión.
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        var data = (ProveedorRespuestaDTO)ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        data.nombreProveedor.Should().Be("Bavaria");
    }

    // Camino 4: excepción del repositorio → 500
    [Fact]
    public async Task GetProveedoresById_RepositorioFalla_Retorna500()
    {
        // Arrange
        repoMock.Setup(r => r.GetProveedoresById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        // Act
        var resultado = await sut.GetProveedoresById(1);

        // Assert
        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // CB-04 · El repositorio guarda → 201 y se llamó a guardar una sola vez
    [Fact]
    public async Task PostProveedores_DatosValidos_GuardaYRetorna201()
    {
        // Arrange
        repoMock.Setup(r => r.PostProveedores(It.IsAny<Proveedores>())).ReturnsAsync(true);
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

        // Act
        var resultado = await sut.PostProveedores(dto);

        // Assert
        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostProveedores(It.Is<Proveedores>(p => p.nombreProveedor == "Bavaria")), Times.Once);
    }

    // CB-05 · El repositorio no guarda → 400
    [Fact]
    public async Task PostProveedores_RepositorioNoGuarda_Retorna400()
    {
        // Arrange
        repoMock.Setup(r => r.PostProveedores(It.IsAny<Proveedores>())).ReturnsAsync(false);
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = "3001234567", direccion = "Cra 1" };

        // Act
        var resultado = await sut.PostProveedores(dto);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    // Rama "if (!ModelState.IsValid)": en una unitaria la validación no corre sola, se fuerza.
    [Fact]
    public async Task PostProveedores_ModeloInvalido_Retorna400SinGuardar()
    {
        // Arrange
        sut.ModelState.AddModelError("telefono", "El campo telefono es obligatorio.");
        var dto = new ProveedorCrearDTO { nombreProveedor = "Bavaria", telefono = "", direccion = "Cra 1" };

        // Act
        var resultado = await sut.PostProveedores(dto);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostProveedores(It.IsAny<Proveedores>()), Times.Never);
    }
}
