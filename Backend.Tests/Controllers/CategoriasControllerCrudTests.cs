using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Categorias;
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
/// Pruebas unitarias (caja blanca) de CategoriasController: cada acción y cada camino
/// (éxito, id inválido, no existe, modelo inválido, fallo del repositorio, integridad 409 y error 500).
/// El repositorio es un doble de Moq: no se toca la base de datos.
/// </summary>
public class CategoriasControllerCrudTests
{
    private readonly Mock<ICategoriasRepository> repoMock = new();
    private readonly CategoriasController sut;

    public CategoriasControllerCrudTests()
    {
        sut = new CategoriasController(repoMock.Object, NullLogger<CategoriasController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    // ---------- GetCategorias ----------

    [Fact]
    public async Task GetCategorias_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetCategorias()).ReturnsAsync(new List<Categorias> { new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" }, new Categorias { idCategoria = 2, nombreCategoria = "Licores" } });

        var resultado = await sut.GetCategorias();

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCategorias_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetCategorias()).ReturnsAsync(new List<Categorias>());

        var resultado = await sut.GetCategorias();

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCategorias_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetCategorias()).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCategorias();

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetCategoriaById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCategoriaById_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.GetCategoriaById(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCategoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCategoriaById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetCategoriasById(99)).ReturnsAsync((Categorias?)null);

        var resultado = await sut.GetCategoriaById(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCategoriaById_Existe_Retorna200()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });

        var resultado = await sut.GetCategoriaById(1);

        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCategoriaById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ThrowsAsync(new Exception("Base de datos caída"));

        var resultado = await sut.GetCategoriaById(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostCategoria ----------

    [Fact]
    public async Task PostCategoria_DatosValidos_GuardaYRetorna200()
    {
        repoMock.Setup(r => r.PostCategorias(It.IsAny<Categorias>())).ReturnsAsync(true);

        var resultado = await sut.PostCategoria(new CategoriaCrearDTO { nombreCategoria = "Cervezas" });

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PostCategorias(It.Is<Categorias>(x => x.nombreCategoria == "Cervezas")), Times.Once);
    }

    [Fact]
    public async Task PostCategoria_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.PostCategorias(It.IsAny<Categorias>())).ReturnsAsync(false);

        var resultado = await sut.PostCategoria(new CategoriaCrearDTO { nombreCategoria = "Cervezas" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostCategoria_ModeloInvalido_Retorna400SinGuardar()
    {
        sut.ModelState.AddModelError("nombreCategoria", "El campo es obligatorio.");

        var resultado = await sut.PostCategoria(new CategoriaCrearDTO { nombreCategoria = "Cervezas" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostCategorias(It.IsAny<Categorias>()), Times.Never);
    }

    [Fact]
    public async Task PostCategoria_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.PostCategorias(It.IsAny<Categorias>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PostCategoria(new CategoriaCrearDTO { nombreCategoria = "Cervezas" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostCategoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.PostCategorias(It.IsAny<Categorias>())).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PostCategoria(new CategoriaCrearDTO { nombreCategoria = "Cervezas" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutCategoria ----------

    [Fact]
    public async Task PutCategoria_DatosValidos_ActualizaYRetorna200()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.PutCategorias(It.IsAny<Categorias>())).ReturnsAsync(true);

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 1, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<OkObjectResult>();
        // Se guarda el registro existente con el valor nuevo
        repoMock.Verify(r => r.PutCategorias(It.Is<Categorias>(x => x.idCategoria == 1 && x.nombreCategoria == "Licores")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutCategoria_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = id, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCategoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PutCategoria_NoExiste_Retorna404SinActualizar()
    {
        repoMock.Setup(r => r.GetCategoriasById(99)).ReturnsAsync((Categorias?)null);

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 99, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.PutCategorias(It.IsAny<Categorias>()), Times.Never);
    }

    [Fact]
    public async Task PutCategoria_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nombreCategoria", "El campo es obligatorio.");

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 1, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PutCategorias(It.IsAny<Categorias>()), Times.Never);
    }

    [Fact]
    public async Task PutCategoria_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.PutCategorias(It.IsAny<Categorias>())).ReturnsAsync(false);

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 1, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutCategoria_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.PutCategorias(It.IsAny<Categorias>())).ThrowsAsync(new DbUpdateException("Restricción de la base"));

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 1, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutCategoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.PutCategoria(new CategoriaActualizarDTO { idCategoria = 1, nombreCategoria = "Licores" });

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteCategoria ----------

    [Fact]
    public async Task DeleteCategoria_Existe_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.DeleteCategorias(It.IsAny<Categorias>())).ReturnsAsync(true);

        var resultado = await sut.DeleteCategoria(1);

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.DeleteCategorias(It.Is<Categorias>(x => x.idCategoria == 1)), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteCategoria_IdNoPositivo_Retorna400SinConsultar(int id)
    {
        var resultado = await sut.DeleteCategoria(id);

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetCategoriasById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoria_NoExiste_Retorna404SinEliminar()
    {
        repoMock.Setup(r => r.GetCategoriasById(99)).ReturnsAsync((Categorias?)null);

        var resultado = await sut.DeleteCategoria(99);

        resultado.Should().BeOfType<NotFoundObjectResult>();
        repoMock.Verify(r => r.DeleteCategorias(It.IsAny<Categorias>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoria_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.DeleteCategorias(It.IsAny<Categorias>())).ReturnsAsync(false);

        var resultado = await sut.DeleteCategoria(1);

        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteCategoria_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ReturnsAsync(new Categorias { idCategoria = 1, nombreCategoria = "Cervezas" });
        repoMock.Setup(r => r.DeleteCategorias(It.IsAny<Categorias>())).ThrowsAsync(new DbUpdateException("Tiene registros asociados"));

        var resultado = await sut.DeleteCategoria(1);

        resultado.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteCategoria_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetCategoriasById(1)).ThrowsAsync(new Exception("Error inesperado"));

        var resultado = await sut.DeleteCategoria(1);

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
