using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Proveedores;
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
/// Pruebas unitarias (caja blanca) de ProveedoresController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class ProveedoresControllerCrudTests
{
    private readonly Mock<IProveedoresRepository> repoMock = new();
    private readonly ProveedoresController sut;

    public ProveedoresControllerCrudTests()
    {
        sut = new ProveedoresController(repoMock.Object, NullLogger<ProveedoresController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetProveedores ----------

    [Fact]
    public async Task GetProveedores_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetProveedores()).ReturnsAsync(new List<Proveedores> { new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" }, new Proveedores { idProveedor = 2, nombreProveedor = "Postobón" } });

        var resultado = await sut.GetProveedores();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProveedores_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetProveedores()).ReturnsAsync(new List<Proveedores>());

        var resultado = await sut.GetProveedores();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProveedores_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetProveedores()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetProveedores();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutProveedores ----------

    [Fact]
    public async Task PutProveedores_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.PutProveedores(It.IsAny<Proveedores>())).ReturnsAsync(true);

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 1, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutProveedores(It.Is<Proveedores>(x => x.idProveedor == 1 && x.nombreProveedor == "Postobón")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutProveedores_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = id, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProveedoresById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutProveedores_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetProveedoresById(99)).ReturnsAsync((Proveedores?)null);

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 99, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutProveedores(It.IsAny<Proveedores>()), Times.Never);
    }

    [Fact]
    public async Task PutProveedores_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreProveedor", "El campo es obligatorio.");

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 1, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutProveedores(It.IsAny<Proveedores>()), Times.Never);
    }

    [Fact]
    public async Task PutProveedores_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.PutProveedores(It.IsAny<Proveedores>())).ReturnsAsync(false);

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 1, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutProveedores_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.PutProveedores(It.IsAny<Proveedores>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 1, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutProveedores_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutProveedores(new ProveedorActualizarDTO { idProveedor = 1, telefono = "3001234567", direccion = "Cra 1", nombreProveedor = "Postobón" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteProveedores ----------

    [Fact]
    public async Task DeleteProveedores_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.DeleteProveedores(It.IsAny<Proveedores>())).ReturnsAsync(true);

        var resultado = await sut.DeleteProveedores(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteProveedores(It.Is<Proveedores>(x => x.idProveedor == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteProveedores_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteProveedores(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetProveedoresById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProveedores_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetProveedoresById(99)).ReturnsAsync((Proveedores?)null);

        var resultado = await sut.DeleteProveedores(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteProveedores(It.IsAny<Proveedores>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProveedores_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.DeleteProveedores(It.IsAny<Proveedores>())).ReturnsAsync(false);

        var resultado = await sut.DeleteProveedores(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteProveedores_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ReturnsAsync(new Proveedores { idProveedor = 1, nombreProveedor = "Bavaria" });
        repoMock.Setup(r => r.DeleteProveedores(It.IsAny<Proveedores>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteProveedores(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteProveedores_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetProveedoresById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteProveedores(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
