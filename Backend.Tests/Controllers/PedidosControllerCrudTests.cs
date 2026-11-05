using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Pedidos;
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
/// Pruebas unitarias (caja blanca) de PedidosController: cada acción CRUD y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// </summary>
public class PedidosControllerCrudTests
{
    private readonly Mock<IPedidosRepository> repoMock = new();
    private readonly PedidosController sut;

    public PedidosControllerCrudTests()
    {
        sut = new PedidosController(repoMock.Object, NullLogger<PedidosController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetPedidos ----------

    [Fact]
    public async Task GetPedidos_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetPedidos()).ReturnsAsync(new List<Pedidos> { new Pedidos { idPedido = 1, estadoPedido = "Pendiente" }, new Pedidos { idPedido = 2, estadoPedido = "Entregado" } });

        var resultado = await sut.GetPedidos();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPedidos_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetPedidos()).ReturnsAsync(new List<Pedidos>());

        var resultado = await sut.GetPedidos();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPedidos_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetPedidos()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetPedidos();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetPedidosById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPedidosById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetPedidosById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPedidosById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetPedidosById(99)).ReturnsAsync((Pedidos?)null);

        var resultado = await sut.GetPedidosById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPedidosById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });

        var resultado = await sut.GetPedidosById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPedidosById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetPedidosById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostPedidos ----------

    [Fact]
    public async Task PostPedidos_DatosValidos_GuardaYRetorna201()
    {
        repoMock.Setup(r => r.PostPedidos(It.IsAny<Pedidos>())).ReturnsAsync(true);

        var resultado = await sut.PostPedidos(new PedidoCrearDTO { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        repoMock.Verify(r => r.PostPedidos(It.Is<Pedidos>(x => x.estadoPedido == "Pendiente")), Times.Once);
    }

    [Fact]
    public async Task PostPedidos_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostPedidos(It.IsAny<Pedidos>())).ReturnsAsync(false);

        var resultado = await sut.PostPedidos(new PedidoCrearDTO { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostPedidos_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("estadoPedido", "El campo no es válido.");

        var resultado = await sut.PostPedidos(new PedidoCrearDTO { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostPedidos(It.IsAny<Pedidos>()), Times.Never);
    }

    [Fact]
    public async Task PostPedidos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostPedidos(It.IsAny<Pedidos>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PostPedidos(new PedidoCrearDTO { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostPedidos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostPedidos(It.IsAny<Pedidos>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostPedidos(new PedidoCrearDTO { idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Pendiente" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutPedidos ----------

    [Fact]
    public async Task PutPedidos_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.PutPedidos(It.IsAny<Pedidos>())).ReturnsAsync(true);

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutPedidos(It.Is<Pedidos>(x => x.idPedido == 1 && x.estadoPedido == "Entregado")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutPedidos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = id, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutPedidos_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetPedidosById(99)).ReturnsAsync((Pedidos?)null);

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 99, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutPedidos(It.IsAny<Pedidos>()), Times.Never);
    }

    [Fact]
    public async Task PutPedidos_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("estadoPedido", "El campo no es válido.");

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutPedidos(It.IsAny<Pedidos>()), Times.Never);
    }

    [Fact]
    public async Task PutPedidos_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.PutPedidos(It.IsAny<Pedidos>())).ReturnsAsync(false);

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutPedidos_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.PutPedidos(It.IsAny<Pedidos>())).ThrowsAsync(new DbUpdateException("Llave foránea inexistente"));

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutPedidos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutPedidos(new PedidoActualizarDTO { idPedido = 1, idCuenta = 1, idUsuario = 1, fecha = new DateTime(2026, 11, 5), estadoPedido = "Entregado" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeletePedidos ----------

    [Fact]
    public async Task DeletePedidos_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.DeletePedidos(It.IsAny<Pedidos>())).ReturnsAsync(true);

        var resultado = await sut.DeletePedidos(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeletePedidos(It.Is<Pedidos>(x => x.idPedido == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeletePedidos_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeletePedidos(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetPedidosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeletePedidos_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetPedidosById(99)).ReturnsAsync((Pedidos?)null);

        var resultado = await sut.DeletePedidos(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeletePedidos(It.IsAny<Pedidos>()), Times.Never);
    }

    [Fact]
    public async Task DeletePedidos_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.DeletePedidos(It.IsAny<Pedidos>())).ReturnsAsync(false);

        var resultado = await sut.DeletePedidos(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeletePedidos_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ReturnsAsync(new Pedidos { idPedido = 1, estadoPedido = "Pendiente" });
        repoMock.Setup(r => r.DeletePedidos(It.IsAny<Pedidos>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeletePedidos(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeletePedidos_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetPedidosById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeletePedidos(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
