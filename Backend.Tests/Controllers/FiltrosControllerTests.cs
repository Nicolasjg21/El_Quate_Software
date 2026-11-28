using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Productos;
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
/// Acciones de filtro de Proveedores y Productos.
/// Hallazgo de caja blanca: estas dos acciones NO tienen try/catch; si el repositorio falla,
/// la excepción sube hasta el ManejadorExcepcionesGlobal (que responde 500).
/// </summary>
public class FiltrosControllerTests
{
    private static ControllerContext Contexto() => new() { HttpContext = new DefaultHttpContext() };

    [Fact]
    public async Task FiltrarProveedores_ConFiltro_Retorna200YUsaElFiltro()
    {
        var repo = new Mock<IProveedoresRepository>();
        var filtro = new ProveedorFiltroDTO();
        repo.Setup(r => r.FiltrarProveedores(filtro)).ReturnsAsync(new List<Proveedores> { new() { idProveedor = 1, nombreProveedor = "Bavaria" } });
        var sut = new ProveedoresController(repo.Object, NullLogger<ProveedoresController>.Instance) { ControllerContext = Contexto() };

        var resultado = await sut.FiltrarProveedores(filtro);

        resultado.Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.FiltrarProveedores(filtro), Times.Once);
    }

    [Fact]
    public async Task FiltrarProveedores_RepositorioFalla_LaExcepcionSubeAlManejadorGlobal()
    {
        var repo = new Mock<IProveedoresRepository>();
        repo.Setup(r => r.FiltrarProveedores(It.IsAny<ProveedorFiltroDTO>())).ThrowsAsync(new Exception("Base de datos caída"));
        var sut = new ProveedoresController(repo.Object, NullLogger<ProveedoresController>.Instance) { ControllerContext = Contexto() };

        var accion = () => sut.FiltrarProveedores(new ProveedorFiltroDTO());

        await accion.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task FiltrarProductos_ConFiltro_Retorna200YUsaElFiltro()
    {
        var repo = new Mock<IProductosRepository>();
        var filtro = new ProductoFiltroDTO { sinStock = true };
        repo.Setup(r => r.FiltrarProductos(filtro)).ReturnsAsync(new List<ProductoRespuestaDTO>());
        var sut = new ProductosController(repo.Object, NullLogger<ProductosController>.Instance) { ControllerContext = Contexto() };

        var resultado = await sut.FiltrarProductos(filtro);

        resultado.Should().BeOfType<OkObjectResult>();
        repo.Verify(r => r.FiltrarProductos(filtro), Times.Once);
    }
}
