using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.DetallePedidos;
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
/// Pruebas unitarias (caja blanca) de DetallePedidosController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class DetallePedidosControllerCrudTests
{
    private readonly Mock<IDetallePedidosRepository> repoMock = new();
    private readonly DetallePedidosController sut;

    public DetallePedidosControllerCrudTests()
    {
        sut = new DetallePedidosController(repoMock.Object, NullLogger<DetallePedidosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetDetallePedidos ----------

    [Fact]
    public async Task GetDetallePedidos_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetDetallePedidos(It.IsAny<int?>())).ReturnsAsync(new List<DetallePedidos> { new DetallePedidos { idDetalle = 1, cantidad = 2 }, new DetallePedidos { idDetalle = 2, cantidad = 3 } });

        var resultado = await sut.GetDetallePedidos();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetallePedidos_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetDetallePedidos(It.IsAny<int?>())).ReturnsAsync(new List<DetallePedidos>());

        var resultado = await sut.GetDetallePedidos();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDetallePedidos_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetDetallePedidos(It.IsAny<int?>())).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetDetallePedidos();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetDetallePedidoById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDetallePedidoById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetDetallePedidoById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetallePedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDetallePedidoById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(99)).ReturnsAsync((DetallePedidos?)null);

        var resultado = await sut.GetDetallePedidoById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDetallePedidoById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });

        var resultado = await sut.GetDetallePedidoById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetallePedidoById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetDetallePedidoById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostDetallePedido ----------

    [Fact]
    public async Task PostDetallePedido_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(true);

        var resultado = await sut.PostDetallePedido(new DetallePedidoCrearDTO { idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostDetallePedidos(It.Is<DetallePedidos>(x => x.cantidad == 2)), Times.Once);
    }

    [Fact]
    public async Task PostDetallePedido_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(false);

        var resultado = await sut.PostDetallePedido(new DetallePedidoCrearDTO { idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostDetallePedido_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("cantidad", "El campo no es válido.");

        var resultado = await sut.PostDetallePedido(new DetallePedidoCrearDTO { idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostDetallePedidos(It.IsAny<DetallePedidos>()), Times.Never);
    }

    [Fact]
    public async Task PostDetallePedido_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostDetallePedidos(It.IsAny<DetallePedidos>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostDetallePedido(new DetallePedidoCrearDTO { idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostDetallePedido_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostDetallePedidos(It.IsAny<DetallePedidos>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostDetallePedido(new DetallePedidoCrearDTO { idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 2 });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutDetallePedido ----------

    [Fact]
    public async Task PutDetallePedido_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.PutDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(true);

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutDetallePedidos(It.Is<DetallePedidos>(x => x.idDetalle == 1 && x.cantidad == 3)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutDetallePedido_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = id, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetallePedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutDetallePedido_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(99)).ReturnsAsync((DetallePedidos?)null);

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 99, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutDetallePedidos(It.IsAny<DetallePedidos>()), Times.Never);
    }

    [Fact]
    public async Task PutDetallePedido_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("cantidad", "El campo no es válido.");

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutDetallePedidos(It.IsAny<DetallePedidos>()), Times.Never);
    }

    [Fact]
    public async Task PutDetallePedido_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.PutDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(false);

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutDetallePedido_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.PutDetallePedidos(It.IsAny<DetallePedidos>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutDetallePedido_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutDetallePedido(new DetallePedidoActualizarDTO { idDetalle = 1, idPedido = 1, idProducto = 1, precioUnitario = 5000m, cantidad = 3 });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteDetallePedido ----------

    [Fact]
    public async Task DeleteDetallePedido_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.DeleteDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(true);

        var resultado = await sut.DeleteDetallePedido(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteDetallePedidos(It.Is<DetallePedidos>(x => x.idDetalle == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteDetallePedido_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteDetallePedido(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetDetallePedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDetallePedido_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(99)).ReturnsAsync((DetallePedidos?)null);

        var resultado = await sut.DeleteDetallePedido(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteDetallePedidos(It.IsAny<DetallePedidos>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDetallePedido_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.DeleteDetallePedidos(It.IsAny<DetallePedidos>())).ReturnsAsync(false);

        var resultado = await sut.DeleteDetallePedido(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteDetallePedido_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ReturnsAsync(new DetallePedidos { idDetalle = 1, cantidad = 2 });
        repoMock.Setup(r => r.DeleteDetallePedidos(It.IsAny<DetallePedidos>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteDetallePedido(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteDetallePedido_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetDetallePedidosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteDetallePedido(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
