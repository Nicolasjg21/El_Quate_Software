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

/// <summary>Pruebas unitarias (caja blanca) de ProductosController con el repositorio simulado.</summary>
public class ProductosControllerTests
{
    private readonly Mock<IProductosRepository> repoMock = new();
    private readonly ProductosController sut;

    public ProductosControllerTests()
    {
        sut = new ProductosController(repoMock.Object, NullLogger<ProductosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Productos Cerveza() => new()
    {
        idProducto = 1, nombreProducto = "Cerveza Águila", precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1
    };

    private static ProductoCrearDTO CervezaDto() => new()
    {
        nombreProducto = "Cerveza Águila", precioVenta = 5000, cantidadMinima = 10, estado = true, idCategoria = 1
    };

    // GetProductosById · camino 1: id no positivo
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetProductosById_IdNoPositivo_Retorna400(int id)
    {
        var resultado = await sut.GetProductosById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProductosById(It.IsAny<int>()), Times.Never);
    }

    // GetProductosById · camino 2: no existe
    [Fact]
    public async Task GetProductosById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetProductosById(99)).ReturnsAsync((Productos?)null);

        var resultado = await sut.GetProductosById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    // GetProductosById · camino 3: existe
    [Fact]
    public async Task GetProductosById_Existe_Retorna200ConElProducto()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(Cerveza());

        var resultado = await sut.GetProductosById(1);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        var data = (ProductoDetalleRespuestaDTO)ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value)!;
        data.nombreProducto.Should().Be("Cerveza Águila");
        data.precioVenta.Should().Be(5000);
    }

    // PostProductos · se guarda → 201
    [Fact]
    public async Task PostProductos_DatosValidos_Retorna201()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ReturnsAsync(true);

        var resultado = await sut.PostProductos(CervezaDto());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostProductos(It.Is<Productos>(p => p.precioVenta == 5000 && p.idCategoria == 1)), Times.Once);
    }

    // PostProductos · categoría inexistente: la base rechaza la llave foránea → 409
    // (este caso NO se puede probar con EF InMemory, que no aplica llaves foráneas; con Moq sí)
    [Fact]
    public async Task PostProductos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostProductos(It.IsAny<Productos>())).ThrowsAsync(new DbUpdateException("FK idCategoria"));

        var resultado = await sut.PostProductos(CervezaDto());

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    // DeleteProductos · producto con ventas asociadas → 409
    [Fact]
    public async Task DeleteProductos_ConRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(Cerveza());
        repoMock.Setup(r => r.DeleteProductos(It.IsAny<Productos>())).ThrowsAsync(new DbUpdateException("FK detallePedidos"));

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    // DeleteProductos · no existe → 404 y no intenta borrar
    [Fact]
    public async Task DeleteProductos_NoExiste_Retorna404SinBorrar()
    {
        repoMock.Setup(r => r.GetProductosById(99)).ReturnsAsync((Productos?)null);

        var resultado = await sut.DeleteProductos(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteProductos(It.IsAny<Productos>()), Times.Never);
    }

    // DeleteProductos · existe → 200
    [Fact]
    public async Task DeleteProductos_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetProductosById(1)).ReturnsAsync(Cerveza());
        repoMock.Setup(r => r.DeleteProductos(It.IsAny<Productos>())).ReturnsAsync(true);

        var resultado = await sut.DeleteProductos(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }
}
