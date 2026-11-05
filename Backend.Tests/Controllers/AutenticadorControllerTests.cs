using System.Security.Claims;
using Backend.Tests.Ayudas;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.DTOs.Usuarios;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// AutenticadorController: la tabla de decisión completa del login (R1 a R5),
/// la protección contra enumeración de usuarios, el rehash y el logout.
/// </summary>
public class AutenticadorControllerTests
{
    private const string mensajeGenerico = "Usuario o contraseña incorrectos.";

    private readonly Mock<IUsuariosRepository> repoMock = new();
    private readonly Mock<IPasswordHasher<Usuarios>> hasherMock = new();
    private readonly Mock<ITokenService> tokenMock = new();
    private readonly Mock<IRastreadorIntentosLogin> rastreadorMock = new();
    private readonly Mock<IRevocacionTokenService> revocacionMock = new();
    private readonly AutenticadorController sut;

    public AutenticadorControllerTests()
    {
        hasherMock.Setup(h => h.HashPassword(It.IsAny<Usuarios>(), It.IsAny<string>())).Returns("HASH");
        tokenMock.Setup(t => t.GenerarToken(It.IsAny<Usuarios>()))
                 .Returns(new TokenResultado { token = "jwt", expiraEn = DateTime.UtcNow.AddMinutes(30) });

        sut = new AutenticadorController(repoMock.Object, hasherMock.Object, tokenMock.Object,
                                         rastreadorMock.Object, revocacionMock.Object,
                                         NullLogger<AutenticadorController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static Usuarios Admin(bool activo = true) => new()
    {
        idUsuario = 1, email = "admin@elcuate.com", passwordHash = "HASH-GUARDADO", estado = activo, idRol = 1
    };

    private void ContrasenaEs(PasswordVerificationResult resultado) =>
        hasherMock.Setup(h => h.VerifyHashedPassword(It.IsAny<Usuarios>(), It.IsAny<string>(), It.IsAny<string>())).Returns(resultado);

    private static LoginDTO Login(string email = "admin@elcuate.com", string password = "Clave123") => new() { email = email, password = password };

    // ---------- Datos obligatorios ----------

    [Theory]
    [InlineData("", "Clave123")]
    [InlineData("   ", "Clave123")]
    [InlineData("admin@elcuate.com", "")]
    [InlineData("admin@elcuate.com", "   ")]
    public async Task Login_CorreoOContrasenaVacios_Retorna400(string email, string password)
    {
        var resultado = await sut.Login(Login(email, password));

        resultado.Should().BeOfType<BadRequestObjectResult>();
        repoMock.Verify(r => r.GetUsuariosByEmail(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_CuerpoNulo_Retorna400()
    {
        (await sut.Login(null!)).Should().BeOfType<BadRequestObjectResult>();
    }

    // ---------- R1: correo bloqueado ----------

    [Fact]
    public async Task Login_CorreoBloqueado_Retorna429SinConsultarLaBase()
    {
        rastreadorMock.Setup(r => r.EstaBloqueado("admin@elcuate.com")).Returns(true);

        var resultado = await sut.Login(Login());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(429);
        repoMock.Verify(r => r.GetUsuariosByEmail(It.IsAny<string>()), Times.Never);
    }

    // ---------- R2: correo inexistente ----------

    [Fact]
    public async Task Login_CorreoInexistente_Retorna401GenericoYRegistraFallo()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail("nadie@elcuate.com")).ReturnsAsync((Usuarios?)null);

        var resultado = await sut.Login(Login("nadie@elcuate.com"));

        resultado.Should().BeOfType<UnauthorizedObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be(mensajeGenerico);
        rastreadorMock.Verify(r => r.RegistrarFallo("nadie@elcuate.com"), Times.Once);
        // Protección contra enumeración por tiempo: igual verifica una contraseña (contra un hash ficticio)
        hasherMock.Verify(h => h.VerifyHashedPassword(It.IsAny<Usuarios>(), It.IsAny<string>(), "Clave123"), Times.Once);
    }

    [Fact]
    public async Task Login_CorreoConEspacios_SeBuscaSinEspacios()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail(It.IsAny<string>())).ReturnsAsync((Usuarios?)null);

        await sut.Login(Login("  admin@elcuate.com  "));

        repoMock.Verify(r => r.GetUsuariosByEmail("admin@elcuate.com"), Times.Once);
    }

    // ---------- R3: contraseña incorrecta ----------

    [Fact]
    public async Task Login_ContrasenaIncorrecta_Retorna401ConElMismoMensajeQueCorreoInexistente()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail("admin@elcuate.com")).ReturnsAsync(Admin());
        ContrasenaEs(PasswordVerificationResult.Failed);

        var resultado = await sut.Login(Login(password: "Mala1234"));

        resultado.Should().BeOfType<UnauthorizedObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be(mensajeGenerico);
        rastreadorMock.Verify(r => r.RegistrarFallo("admin@elcuate.com"), Times.Once);
        tokenMock.Verify(t => t.GenerarToken(It.IsAny<Usuarios>()), Times.Never);
    }

    // ---------- R4: contraseña correcta, usuario inactivo ----------

    [Fact]
    public async Task Login_UsuarioInactivo_Retorna401InactivoSinToken()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail("admin@elcuate.com")).ReturnsAsync(Admin(activo: false));
        ContrasenaEs(PasswordVerificationResult.Success);

        var resultado = await sut.Login(Login());

        resultado.Should().BeOfType<UnauthorizedObjectResult>();
        Respuestas.Leer(resultado, "message").Should().Be("El usuario se encuentra inactivo.");
        tokenMock.Verify(t => t.GenerarToken(It.IsAny<Usuarios>()), Times.Never);
    }

    // ---------- R5: todo correcto ----------

    [Fact]
    public async Task Login_CredencialesCorrectas_Retorna200ConTokenYLimpiaIntentos()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail("admin@elcuate.com")).ReturnsAsync(Admin());
        ContrasenaEs(PasswordVerificationResult.Success);

        var resultado = await sut.Login(Login());

        resultado.Should().BeOfType<OkObjectResult>();
        Respuestas.Leer(resultado, "token").Should().Be("jwt");
        rastreadorMock.Verify(r => r.Limpiar("admin@elcuate.com"), Times.Once);
        repoMock.Verify(r => r.PutUsuarios(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task Login_HashAntiguo_SeActualizaElHashYEntraNormal()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail("admin@elcuate.com")).ReturnsAsync(Admin());
        ContrasenaEs(PasswordVerificationResult.SuccessRehashNeeded);

        var resultado = await sut.Login(Login());

        resultado.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.PutUsuarios(It.Is<Usuarios>(u => u.passwordHash == "HASH")), Times.Once);
    }

    [Fact]
    public async Task Login_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosByEmail(It.IsAny<string>())).ThrowsAsync(new Exception("Base de datos caída"));

        (await sut.Login(Login())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ---------- Logout ----------

    [Fact]
    public void Logout_TokenConSesionYVencimiento_RevocaLaSesion()
    {
        var vence = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds();
        sut.ControllerContext = Respuestas.ContextoConUsuario(1,
            new Claim(SelloSeguridad.claimSesion, "sesion-abc"),
            new Claim("exp", vence.ToString()));

        var resultado = sut.Logout();

        resultado.Should().BeOfType<OkObjectResult>();
        revocacionMock.Verify(r => r.Revocar("sesion-abc", DateTimeOffset.FromUnixTimeSeconds(vence)), Times.Once);
    }

    [Fact]
    public void Logout_TokenSinSesion_Retorna200SinRevocar()
    {
        sut.ControllerContext = Respuestas.ContextoConUsuario(1);

        sut.Logout().Should().BeOfType<OkObjectResult>();
        revocacionMock.Verify(r => r.Revocar(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }
}
