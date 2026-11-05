using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.DetalleCompras;
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
/// Pruebas unitarias (caja blanca) de DetalleComprasController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class DetalleComprasControllerCrudTests
{
    private readonly Mock<IDetalleComprasRepository> repoMock = new();
    private readonly DetalleComprasController sut;

    public DetalleComprasControllerCrudTests()
    {
        sut = new DetalleComprasController(repoMock.Object, NullLogger<DetalleComprasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetDetalleCompras ----------

    [Fact]
    public async Task GetDetalleCompras_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetDetalleCompras()).ReturnsAsync(new List<DetalleCompras> { new DetalleCompras { idDetalleCompra = 1, cantidad = 24 }, new DetalleCompras { idDetalleCompra = 2, cantidad = 48 } });

        var resultado = await sut.GetDetalleCompras();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetalleCompras_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetDetalleCompras()).ReturnsAsync(new List<DetalleCompras>());

        var resultado = await sut.GetDetalleCompras();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDetalleCompras_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetDetalleCompras()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetDetalleCompras();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetDetalleCompraById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDetalleCompraById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetDetalleCompraById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetalleComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDetalleCompraById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(99)).ReturnsAsync((DetalleCompras?)null);

        var resultado = await sut.GetDetalleCompraById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDetalleCompraById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });

        var resultado = await sut.GetDetalleCompraById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetalleCompraById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetDetalleCompraById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostDetalleCompra ----------

    [Fact]
    public async Task PostDetalleCompra_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(true);

        var resultado = await sut.PostDetalleCompra(new DetalleCompraCrearDTO { idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostDetalleCompras(It.Is<DetalleCompras>(x => x.cantidad == 24)), Times.Once);
    }

    [Fact]
    public async Task PostDetalleCompra_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(false);

        var resultado = await sut.PostDetalleCompra(new DetalleCompraCrearDTO { idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostDetalleCompra_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("cantidad", "El campo no es válido.");

        var resultado = await sut.PostDetalleCompra(new DetalleCompraCrearDTO { idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostDetalleCompras(It.IsAny<DetalleCompras>()), Times.Never);
    }

    [Fact]
    public async Task PostDetalleCompra_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostDetalleCompras(It.IsAny<DetalleCompras>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostDetalleCompra(new DetalleCompraCrearDTO { idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostDetalleCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostDetalleCompras(It.IsAny<DetalleCompras>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostDetalleCompra(new DetalleCompraCrearDTO { idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 24 });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutDetalleCompra ----------

    [Fact]
    public async Task PutDetalleCompra_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.PutDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(true);

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutDetalleCompras(It.Is<DetalleCompras>(x => x.idDetalleCompra == 1 && x.cantidad == 48)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutDetalleCompra_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = id, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetalleComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutDetalleCompra_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(99)).ReturnsAsync((DetalleCompras?)null);

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 99, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutDetalleCompras(It.IsAny<DetalleCompras>()), Times.Never);
    }

    [Fact]
    public async Task PutDetalleCompra_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("cantidad", "El campo no es válido.");

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutDetalleCompras(It.IsAny<DetalleCompras>()), Times.Never);
    }

    [Fact]
    public async Task PutDetalleCompra_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.PutDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(false);

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutDetalleCompra_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.PutDetalleCompras(It.IsAny<DetalleCompras>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutDetalleCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutDetalleCompra(new DetalleCompraActualizarDTO { idDetalleCompra = 1, idCompra = 1, idProducto = 1, precioCompra = 3000m, cantidad = 48 });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteDetalleCompra ----------

    [Fact]
    public async Task DeleteDetalleCompra_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.DeleteDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(true);

        var resultado = await sut.DeleteDetalleCompra(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteDetalleCompras(It.Is<DetalleCompras>(x => x.idDetalleCompra == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteDetalleCompra_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteDetalleCompra(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetalleComprasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDetalleCompra_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(99)).ReturnsAsync((DetalleCompras?)null);

        var resultado = await sut.DeleteDetalleCompra(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteDetalleCompras(It.IsAny<DetalleCompras>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDetalleCompra_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.DeleteDetalleCompras(It.IsAny<DetalleCompras>())).ReturnsAsync(false);

        var resultado = await sut.DeleteDetalleCompra(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteDetalleCompra_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ReturnsAsync(new DetalleCompras { idDetalleCompra = 1, cantidad = 24 });
        repoMock.Setup(r => r.DeleteDetalleCompras(It.IsAny<DetalleCompras>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteDetalleCompra(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteDetalleCompra_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetDetalleComprasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteDetalleCompra(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
