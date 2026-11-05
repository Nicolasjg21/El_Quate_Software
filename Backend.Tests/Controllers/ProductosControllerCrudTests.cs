using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Productos;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Pruebas unitarias (caja blanca) de ProductosController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class ProductosControllerCrudTests
{
    private readonly Mock<IProductosRepository> repoMock = new();
    private readonly ProductosController sut;

    public ProductosControllerCrudTests()
    {
        sut = new ProductosController(repoMock.Object, NullLogger<ProductosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetProductos ----------

    [Fact]
    public async Task GetProductos_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetProductos()).ReturnsAsync(new List<Productos> { new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" }, new Productos { idProducto = 2, nombreProducto = "Cerveza Poker" } });

        var resultado = await sut.GetProductos();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProductos_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetProductos()).ReturnsAsync(new List<Productos>());

        var resultado = await sut.GetProductos();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProductos_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetProductos()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetProductos();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostProductos ----------

    [Fact]
    public async Task PostProductos_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ReturnsAsync(true);

        var resultado = await sut.PostProductos(new ProductoCrearDTO { precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostProductos(It.Is<Productos>(x => x.nombreProducto == "Cerveza Águila")), Times.Once);
    }

    [Fact]
    public async Task PostProductos_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ReturnsAsync(false);

        var resultado = await sut.PostProductos(new ProductoCrearDTO { precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostProductos_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombreProducto", "El campo es obligatorio.");

        var resultado = await sut.PostProductos(new ProductoCrearDTO { precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostProductos(It.IsAny<Productos>()), Times.Never);
    }

    [Fact]
    public async Task PostProductos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostProductos(new ProductoCrearDTO { precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostProductos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostProductos(new ProductoCrearDTO { precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Águila" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutProductos ----------

    [Fact]
    public async Task PutProductos_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.PutProductos(It.IsAny<Productos>())).ReturnsAsync(true);

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 1, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutProductos(It.Is<Productos>(x => x.idProducto == 1 && x.nombreProducto == "Cerveza Poker")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutProductos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = id, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProductosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutProductos_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetProductosById(99)).ReturnsAsync((Productos?)null);

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 99, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutProductos(It.IsAny<Productos>()), Times.Never);
    }

    [Fact]
    public async Task PutProductos_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreProducto", "El campo es obligatorio.");

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 1, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutProductos(It.IsAny<Productos>()), Times.Never);
    }

    [Fact]
    public async Task PutProductos_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.PutProductos(It.IsAny<Productos>())).ReturnsAsync(false);

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 1, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutProductos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.PutProductos(It.IsAny<Productos>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 1, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutProductos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutProductos(new ProductoActualizarDTO { idProducto = 1, precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1, nombreProducto = "Cerveza Poker" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteProductos ----------

    [Fact]
    public async Task DeleteProductos_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.DeleteProductos(It.IsAny<Productos>())).ReturnsAsync(true);

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteProductos(It.Is<Productos>(x => x.idProducto == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteProductos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteProductos(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProductosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductos_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetProductosById(99)).ReturnsAsync((Productos?)null);

        var resultado = await sut.DeleteProductos(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteProductos(It.IsAny<Productos>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductos_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.DeleteProductos(It.IsAny<Productos>())).ReturnsAsync(false);

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteProductos_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(new Productos { idProducto = 1, nombreProducto = "Cerveza Águila" });
        repoMock.Setup(r => r.DeleteProductos(It.IsAny<Productos>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteProductos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
