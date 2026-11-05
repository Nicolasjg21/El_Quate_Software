using System.Security.Claims;
using Backend.Tests.Ayudas;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// UsuariosController: CRUD, correo único, contraseña con hash, cambio de contraseña
/// (propietario o administrador) y protección contra eliminarse a sí mismo.
/// El usuario autenticado en estas pruebas tiene id 1.
/// </summary>
public class UsuariosControllerTests
{
    private readonly Mock<IUsuariosRepository> repoMock = new();
    private readonly Mock<IPasswordHasher<Usuarios>> hasherMock = new();
    private readonly Mock<IAuthorizationService> authMock = new();
    private readonly Mock<ITokenService> tokenMock = new();
    private readonly UsuariosController sut;

    public UsuariosControllerTests()
    {
        hasherMock.Setup(h => h.HashPassword(It.IsAny<Usuarios>(), It.IsAny<string>())).Returns("HASH-NUEVO");
        tokenMock.Setup(t => t.GenerarToken(It.IsAny<Usuarios>()))
                 .Returns(new TokenResultado { token = "jwt-nuevo", expiraEn = DateTime.UtcNow.AddMinutes(30) });
        EsAdministrador(false);

        sut = new UsuariosController(repoMock.Object, hasherMock.Object, authMock.Object, tokenMock.Object,
                                     NullLogger<UsuariosController>.Instance)
        {
            ControllerContext = Respuestas.ContextoConUsuario(1)
        };
    }

    private void EsAdministrador(bool valor) =>
        authMock.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(valor ? AuthorizationResult.Success() : AuthorizationResult.Failed());

    private static Usuarios Laura(int id = 5) => new()
    {
        idUsuario = id, nombres = "Laura", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1,
        telefono = "300", passwordHash = "HASH-VIEJO", estado = true, idRol = 2, email = "laura@elcuate.com"
    };

    private static UsuarioCrearDTO NuevoDto() => new()
    {
        nombres = "Laura", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "300",
        password = "ClaveSegura1", estado = true, idRol = 2, email = "laura@elcuate.com"
    };

    private static UsuarioActualizarDTO EditarDto() => new()
    {
        nombres = "Laura M.", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "301",
        estado = true, idRol = 3, email = "laura.m@elcuate.com"
    };

    // ---------- GetUsuarios ----------

    [Fact]
    public async Task GetUsuarios_ConDatos_Retorna200()
    {
        repoMock.Setup(r => r.GetUsuarios()).ReturnsAsync(new List<Usuarios> { Laura() });
        (await sut.GetUsuarios()).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUsuarios_ListaVacia_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuarios()).ReturnsAsync(new List<Usuarios>());
        (await sut.GetUsuarios()).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsuarios_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuarios()).ThrowsAsync(new Exception("Base de datos caída"));
        (await sut.GetUsuarios()).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- GetUsuariosById ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetUsuariosById_IdNoPositivo_Retorna400(int id)
    {
        (await sut.GetUsuariosById(id)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUsuariosById_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(99)).ReturnsAsync((Usuarios?)null);
        (await sut.GetUsuariosById(99)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUsuariosById_Existe_Retorna200SinExponerLaContrasena()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());

        var resultado = await sut.GetUsuariosById(5);

        resultado.Should().BeOfType<OkObjectResult>();
        var data = (UsuarioRespuestaDTO)Respuestas.Leer(resultado, "data")!;
        data.email.Should().Be("laura@elcuate.com");
        data.GetType().GetProperties().Select(p => p.Name.ToLower()).Should().NotContain(n => n.Contains("password"));
    }

    [Fact]
    public async Task GetUsuariosById_RepositorioFalla_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("Base de datos caída"));
        (await sut.GetUsuariosById(5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PostUsuarios ----------

    [Fact]
    public async Task PostUsuarios_DatosValidos_GuardaConHashYRetorna201()
    {
        repoMock.Setup(r => r.ExisteEmail("laura@elcuate.com", It.IsAny<int?>())).ReturnsAsync(false);
        repoMock.Setup(r => r.PostUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(true);

        var resultado = await sut.PostUsuarios(NuevoDto());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        // La contraseña se guarda como hash, nunca en texto plano
        hasherMock.Verify(h => h.HashPassword(It.IsAny<Usuarios>(), "ClaveSegura1"), Times.Once);
        repoMock.Verify(r => r.PostUsuarios(It.Is<Usuarios>(u => u.passwordHash == "HASH-NUEVO" && u.email == "laura@elcuate.com")), Times.Once);
    }

    [Fact]
    public async Task PostUsuarios_CorreoYaRegistrado_Retorna409SinGuardar()
    {
        repoMock.Setup(r => r.ExisteEmail("laura@elcuate.com", It.IsAny<int?>())).ReturnsAsync(true);

        var resultado = await sut.PostUsuarios(NuevoDto());

        resultado.Should().BeOfType<ConflictObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be("Ya existe un usuario registrado con ese correo electrónico.");
        repoMock.Verify(r => r.PostUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task PostUsuarios_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("password", "La contraseña debe tener entre 8 y 128 caracteres");

        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.PostUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task PostUsuarios_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        repoMock.Setup(r => r.PostUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(false);

        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostUsuarios_RolInexistente_Retorna409()
    {
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        repoMock.Setup(r => r.PostUsuarios(It.IsAny<Usuarios>())).ThrowsAsync(new DbUpdateException("FK idRol"));

        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ThrowsAsync(new Exception("Error"));

        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- PutUsuarios ----------

    [Fact]
    public async Task PutUsuarios_DatosValidos_ActualizaSinTocarLaContrasena()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.ExisteEmail("laura.m@elcuate.com", 5)).ReturnsAsync(false);
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(true);

        var resultado = await sut.PutUsuarios(5, EditarDto());

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutUsuarios(It.Is<Usuarios>(u =>
            u.idUsuario == 5 && u.email == "laura.m@elcuate.com" && u.idRol == 3 && u.passwordHash == "HASH-VIEJO")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutUsuarios_IdNoPositivo_Retorna400(int id)
    {
        (await sut.PutUsuarios(id, EditarDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("email", "El correo electrónico no es válido");
        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(99)).ReturnsAsync((Usuarios?)null);
        (await sut.PutUsuarios(99, EditarDto())).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_CorreoDeOtroUsuario_Retorna409SinActualizar()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.ExisteEmail("laura.m@elcuate.com", 5)).ReturnsAsync(true);

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ConflictObjectResult>();
        repoMock.Verify(r => r.PutUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task PutUsuarios_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(false);

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_ViolaIntegridad_Retorna409()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("Error"));
        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- CambiarPassword ----------

    private static CambiarPasswordDTO Cambio(string actual = "ClaveVieja1", string nueva = "ClaveNueva1", string confirmar = "ClaveNueva1") =>
        new() { passwordActual = actual, nuevaPassword = nueva, confirmarPassword = confirmar };

    private void ContrasenaActualEs(PasswordVerificationResult resultado) =>
        hasherMock.Setup(h => h.VerifyHashedPassword(It.IsAny<Usuarios>(), It.IsAny<string>(), It.IsAny<string>())).Returns(resultado);

    [Fact]
    public async Task CambiarPassword_PropioUsuario_ActualizaYEntregaTokenNuevo()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ReturnsAsync(Laura(1));
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(true);
        ContrasenaActualEs(PasswordVerificationResult.Success);

        var resultado = await sut.CambiarPassword(1, Cambio());

        resultado.Should().BeOfType<OkObjectResult>();
        Respuestas.Leer(resultado, "token").Should().Be("jwt-nuevo");
        repoMock.Verify(r => r.PutUsuarios(It.Is<Usuarios>(u => u.passwordHash == "HASH-NUEVO")), Times.Once);
    }

    [Fact]
    public async Task CambiarPassword_AdministradorAOtroUsuario_ActualizaSinEntregarToken()
    {
        EsAdministrador(true);
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura(5));
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(true);
        ContrasenaActualEs(PasswordVerificationResult.Success);

        var resultado = await sut.CambiarPassword(5, Cambio());

        resultado.Should().BeOfType<OkObjectResult>();
        Respuestas.Leer(resultado, "token").Should().BeNull();
        tokenMock.Verify(t => t.GenerarToken(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPassword_OtroUsuarioSinSerAdministrador_Retorna403()
    {
        var resultado = await sut.CambiarPassword(5, Cambio());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        repoMock.Verify(r => r.GetUsuariosById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPassword_ContrasenaActualIncorrecta_Retorna400SinCambiar()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ReturnsAsync(Laura(1));
        ContrasenaActualEs(PasswordVerificationResult.Failed);

        var resultado = await sut.CambiarPassword(1, Cambio());

        resultado.Should().BeOfType<BadRequestObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be("La contraseña actual es incorrecta.");
        repoMock.Verify(r => r.PutUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPassword_ConfirmacionNoCoincide_Retorna400()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ReturnsAsync(Laura(1));
        ContrasenaActualEs(PasswordVerificationResult.Success);

        var resultado = await sut.CambiarPassword(1, Cambio(confirmar: "OtraClave1"));

        resultado.Should().BeOfType<BadRequestObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be("La nueva contraseña y la confirmación no coinciden.");
    }

    [Fact]
    public async Task CambiarPassword_UsuarioNoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ReturnsAsync((Usuarios?)null);
        (await sut.CambiarPassword(1, Cambio())).Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CambiarPassword_IdNoPositivo_Retorna400(int id)
    {
        (await sut.CambiarPassword(id, Cambio())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nuevaPassword", "Obligatoria");
        (await sut.CambiarPassword(1, Cambio())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_RepositorioNoActualiza_Retorna400()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ReturnsAsync(Laura(1));
        repoMock.Setup(r => r.PutUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(false);
        ContrasenaActualEs(PasswordVerificationResult.Success);

        (await sut.CambiarPassword(1, Cambio())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(1)).ThrowsAsync(new Exception("Error"));
        (await sut.CambiarPassword(1, Cambio())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- DeleteUsuarios ----------

    [Fact]
    public async Task DeleteUsuarios_OtroUsuario_EliminaYRetorna200()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.DeleteUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(true);

        (await sut.DeleteUsuarios(5)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_SuPropioUsuario_Retorna400SinEliminar()
    {
        var resultado = await sut.DeleteUsuarios(1);   // el usuario autenticado es el 1

        resultado.Should().BeOfType<BadRequestObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be("No puede eliminar su propio usuario.");
        repoMock.Verify(r => r.DeleteUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeleteUsuarios_IdNoPositivo_Retorna400(int id)
    {
        (await sut.DeleteUsuarios(id)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(99)).ReturnsAsync((Usuarios?)null);
        (await sut.DeleteUsuarios(99)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_RepositorioNoElimina_Retorna400()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.DeleteUsuarios(It.IsAny<Usuarios>())).ReturnsAsync(false);
        (await sut.DeleteUsuarios(5)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_TieneRegistrosAsociados_Retorna409()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ReturnsAsync(Laura());
        repoMock.Setup(r => r.DeleteUsuarios(It.IsAny<Usuarios>())).ThrowsAsync(new DbUpdateException("Tiene pedidos"));
        (await sut.DeleteUsuarios(5)).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("Error"));
        (await sut.DeleteUsuarios(5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
