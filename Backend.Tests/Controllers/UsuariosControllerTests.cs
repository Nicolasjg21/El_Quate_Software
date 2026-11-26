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
/// UsuariosController (bloque 2.12): reglas de quién administra a quién, auditoría en la misma
/// transacción, perfil propio, cambio y restablecimiento de contraseña.
/// Soporte = 15, administradora principal = 1 (usuario de la sesión por defecto), otro administrador = 7.
/// Rol 1 = administrador (tiene usuarios.gestionar); roles 2 y 3 = empleados.
/// </summary>
public class UsuariosControllerTests
{
    private const int soporte = 15, patricia = 1, otroAdmin = 7;

    private readonly Mock<IUsuariosRepository> repoMock = new();
    private readonly Mock<IPasswordHasher<Usuarios>> hasherMock = new();
    private readonly Mock<IAuthorizationService> authMock = new();
    private readonly Mock<ITokenService> tokenMock = new();
    private readonly Mock<IPermisosRolService> permisosMock = new();
    private readonly ReglasUsuarios reglas = new(new UsuariosProtegidosSettings { idSoporte = soporte, idAdministradora = patricia });
    private readonly UsuariosController sut;

    private Usuarios? guardado;
    private Auditorias? auditoria;

    public UsuariosControllerTests()
    {
        hasherMock.Setup(h => h.HashPassword(It.IsAny<Usuarios>(), It.IsAny<string>())).Returns("HASH-NUEVO");
        ContrasenaEs(PasswordVerificationResult.Success);
        tokenMock.Setup(t => t.GenerarToken(It.IsAny<Usuarios>()))
                 .Returns(new TokenResultado { token = "jwt-nuevo", expiraEn = DateTime.UtcNow.AddMinutes(60) });
        permisosMock.Setup(p => p.TienePermiso(It.IsAny<int>(), PermisosSistema.usuariosGestionar))
                    .ReturnsAsync((int idRol, string _) => idRol == 1);
        Permisos(usuarios: true, auditorias: false);

        repoMock.Setup(r => r.CrearConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()))
                .Callback<Usuarios, Auditorias>((u, a) => { guardado = u; auditoria = a; }).ReturnsAsync(true);
        repoMock.Setup(r => r.ActualizarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()))
                .Callback<Usuarios, Auditorias>((u, a) => { guardado = u; auditoria = a; }).ReturnsAsync(true);
        repoMock.Setup(r => r.EliminarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()))
                .Callback<Usuarios, Auditorias>((u, a) => { guardado = u; auditoria = a; }).ReturnsAsync(true);

        sut = new UsuariosController(repoMock.Object, hasherMock.Object, authMock.Object, tokenMock.Object,
                                     permisosMock.Object, reglas, NullLogger<UsuariosController>.Instance)
        {
            ControllerContext = Respuestas.ContextoConUsuario(patricia)
        };
    }

    private void Actor(int id) => sut.ControllerContext = Respuestas.ContextoConUsuario(id);

    private void Permisos(bool usuarios, bool auditorias)
    {
        authMock.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), PoliticasAutorizacion.Permiso(PermisosSistema.usuariosGestionar)))
                .ReturnsAsync(usuarios ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        authMock.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), PoliticasAutorizacion.Permiso(PermisosSistema.auditoriasConsultar)))
                .ReturnsAsync(auditorias ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }

    private void ContrasenaEs(PasswordVerificationResult r) =>
        hasherMock.Setup(h => h.VerifyHashedPassword(It.IsAny<Usuarios>(), It.IsAny<string>(), It.IsAny<string>())).Returns(r);

    private void Existe(Usuarios u) => repoMock.Setup(r => r.GetUsuariosById(u.idUsuario)).ReturnsAsync(u);

    private static Usuarios Usuario(int id, int idRol = 2, string nombre = "Laura") => new()
    {
        idUsuario = id, nombres = nombre, apellidos = "Gómez", documento = "1020", idTipoDocumento = 1,
        telefono = "300", passwordHash = "HASH-VIEJO", estado = true, idRol = idRol, email = $"u{id}@elcuate.com"
    };

    private static UsuarioCrearDTO NuevoDto(int idRol = 2) => new()
    {
        nombres = " Laura ", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "300",
        password = "ClaveSegura1", estado = true, idRol = idRol, email = "  laura@elcuate.com "
    };

    private static UsuarioActualizarDTO EditarDto(int idRol = 3) => new()
    {
        nombres = "Laura M.", apellidos = "Gómez", documento = "1020", idTipoDocumento = 1, telefono = "301",
        estado = true, idRol = idRol, email = "laura.m@elcuate.com"
    };

    private static CambiarPasswordDTO Cambio(string actual = "ClaveVieja1", string nueva = "ClaveNueva1", string? confirmar = null) =>
        new() { passwordActual = actual, nuevaPassword = nueva, confirmarPassword = confirmar ?? nueva };

    private static void Es403(IActionResult r) => r.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);

    private void NadaSeGuardo()
    {
        repoMock.Verify(r => r.CrearConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()), Times.Never);
        repoMock.Verify(r => r.ActualizarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()), Times.Never);
        repoMock.Verify(r => r.EliminarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>()), Times.Never);
    }

    // =====================================================================
    // GetUsuarios
    // =====================================================================

    [Fact]
    public async Task GetUsuarios_ConUsuariosGestionar_DevuelveLasAccionesPermitidasPorUsuario()
    {
        repoMock.Setup(r => r.GetUsuarios()).ReturnsAsync(new List<Usuarios> { Usuario(5), Usuario(soporte, 1, "Soporte") });

        var resultado = await sut.GetUsuarios();

        var data = (List<UsuarioConAccionesDTO>)Respuestas.Leer(resultado, "data")!;
        var laura = data.Single(u => u.idUsuario == 5).acciones;
        var soporteAcciones = data.Single(u => u.idUsuario == soporte).acciones;
        (laura.editar, laura.eliminar, laura.restablecerPassword).Should().Be((true, true, true));
        (soporteAcciones.editar, soporteAcciones.eliminar, soporteAcciones.restablecerPassword).Should().Be((false, false, false));
    }

    [Fact]
    public async Task GetUsuarios_SoloAuditoria_ConsultaSinAcciones()
    {
        Permisos(usuarios: false, auditorias: true);
        repoMock.Setup(r => r.GetUsuarios()).ReturnsAsync(new List<Usuarios> { Usuario(5) });

        var resultado = await sut.GetUsuarios();

        var acciones = ((List<UsuarioConAccionesDTO>)Respuestas.Leer(resultado, "data")!).Single().acciones;
        (acciones.editar || acciones.eliminar || acciones.restablecerPassword).Should().BeFalse();
    }

    [Fact]
    public async Task GetUsuarios_SinPermisos_Retorna403()
    {
        Permisos(usuarios: false, auditorias: false);

        Es403(await sut.GetUsuarios());
        repoMock.Verify(r => r.GetUsuarios(), Times.Never);
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
        repoMock.Setup(r => r.GetUsuarios()).ThrowsAsync(new Exception("BD caída"));
        (await sut.GetUsuarios()).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // =====================================================================
    // GetUsuariosById
    // =====================================================================

    [Fact]
    public async Task GetUsuariosById_PropioSinPermiso_Retorna200SinExponerLaContrasena()
    {
        Permisos(usuarios: false, auditorias: false);
        Existe(Usuario(patricia, 1));

        var resultado = await sut.GetUsuariosById(patricia);

        var data = Respuestas.Leer(resultado, "data")!;
        data.Should().BeOfType<UsuarioRespuestaDTO>();
        data.GetType().GetProperty("passwordHash").Should().BeNull();
    }

    [Fact]
    public async Task GetUsuariosById_OtroSinPermiso_Retorna403()
    {
        Permisos(usuarios: false, auditorias: false);

        Es403(await sut.GetUsuariosById(5));
    }

    [Fact]
    public async Task GetUsuariosById_OtroConPermiso_Retorna200()
    {
        Existe(Usuario(5));
        (await sut.GetUsuariosById(5)).Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
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

    // =====================================================================
    // MiPerfil
    // =====================================================================

    [Theory]
    [InlineData(patricia, true)]
    [InlineData(5, true)]
    [InlineData(soporte, false)]
    public async Task GetMiPerfil_IndicaSiPuedeEditarElTelefono(int actor, bool puede)
    {
        Actor(actor);
        Existe(Usuario(actor));

        var resultado = await sut.GetMiPerfil();

        resultado.Should().BeOfType<OkObjectResult>();
        Respuestas.Leer(resultado, "puedeEditarTelefono").Should().Be(puede);
    }

    [Fact]
    public async Task PutMiPerfil_CambiaSoloElTelefonoYAudita()
    {
        Actor(5);
        Existe(Usuario(5));

        var resultado = await sut.PutMiPerfil(new MiPerfilActualizarDTO { telefono = " 3011234567 " });

        resultado.Should().BeOfType<OkObjectResult>();
        guardado!.telefono.Should().Be("3011234567");
        guardado.nombres.Should().Be("Laura");
        auditoria!.accion.Should().Be("UPDATE");
        auditoria.idUsuario.Should().Be(5);
        auditoria.datosAnteriores.Should().Contain("\"300\"");
        auditoria.datosNuevos.Should().Contain("3011234567");
    }

    [Fact]
    public async Task PutMiPerfil_Soporte_Retorna403SinGuardar()
    {
        Actor(soporte);

        Es403(await sut.PutMiPerfil(new MiPerfilActualizarDTO { telefono = "300" }));
        NadaSeGuardo();
    }

    [Fact]
    public async Task PutMiPerfil_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("telefono", "requerido");
        (await sut.PutMiPerfil(new MiPerfilActualizarDTO())).Should().BeOfType<BadRequestObjectResult>();
    }

    // =====================================================================
    // PostUsuarios
    // =====================================================================

    [Fact]
    public async Task PostUsuarios_DatosValidos_GuardaConHashYAuditoriaEnLaMismaOperacion()
    {
        var resultado = await sut.PostUsuarios(NuevoDto());

        resultado.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
        guardado!.passwordHash.Should().Be("HASH-NUEVO");
        guardado.email.Should().Be("laura@elcuate.com");
        guardado.nombres.Should().Be("Laura");
        auditoria!.tabla.Should().Be("Usuarios");
        auditoria.accion.Should().Be("INSERT");
        auditoria.idUsuario.Should().Be(patricia);
        auditoria.datosNuevos.Should().Contain("laura@elcuate.com").And.NotContain("HASH").And.NotContain("ClaveSegura1");
    }

    [Fact]
    public async Task PostUsuarios_OtroAdministradorCreaUnAdministrador_Retorna403()
    {
        Actor(otroAdmin);

        Es403(await sut.PostUsuarios(NuevoDto(idRol: 1)));
        NadaSeGuardo();
    }

    [Fact]
    public async Task PostUsuarios_OtroAdministradorCreaUnEmpleado_Retorna201()
    {
        Actor(otroAdmin);

        (await sut.PostUsuarios(NuevoDto(idRol: 3))).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task PostUsuarios_CorreoYaRegistrado_Retorna409SinGuardar()
    {
        repoMock.Setup(r => r.ExisteEmail("laura@elcuate.com", null)).ReturnsAsync(true);

        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<ConflictObjectResult>();
        NadaSeGuardo();
    }

    [Fact]
    public async Task PostUsuarios_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("email", "inválido");
        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostUsuarios_RepositorioNoGuarda_Retorna400()
    {
        repoMock.Setup(r => r.CrearConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ReturnsAsync(false);
        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PostUsuarios_IndiceUnicoOLlaveForanea_Retorna409()
    {
        repoMock.Setup(r => r.CrearConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("UX_Usuarios_email"));
        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PostUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.ExisteEmail(It.IsAny<string>(), It.IsAny<int?>())).ThrowsAsync(new Exception("x"));
        (await sut.PostUsuarios(NuevoDto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // =====================================================================
    // PutUsuarios
    // =====================================================================

    [Fact]
    public async Task PutUsuarios_AdministradoraEditaEmpleado_ActualizaConAuditoriaAntesYDespues()
    {
        Existe(Usuario(5));

        var resultado = await sut.PutUsuarios(5, EditarDto());

        resultado.Should().BeOfType<OkObjectResult>();
        guardado!.idRol.Should().Be(3);
        guardado.passwordHash.Should().Be("HASH-VIEJO");   // editar no toca la contraseña
        auditoria!.accion.Should().Be("UPDATE");
        auditoria.datosAnteriores.Should().Contain("u5@elcuate.com");
        auditoria.datosNuevos.Should().Contain("laura.m@elcuate.com");
    }

    [Theory]
    [InlineData(patricia, soporte, 1, 1)]      // Soporte no lo edita nadie
    [InlineData(soporte, soporte, 1, 1)]       // ni él mismo
    [InlineData(otroAdmin, patricia, 1, 1)]    // a la administradora solo la edita Soporte
    [InlineData(otroAdmin, 8, 1, 1)]           // un administrador no edita a otro administrador
    [InlineData(otroAdmin, 5, 2, 1)]           // ni vuelve administrador a un empleado
    [InlineData(5, 5, 2, 2)]                   // nadie se edita a sí mismo desde aquí
    public async Task PutUsuarios_ReglaIncumplida_Retorna403SinActualizar(int actor, int objetivo, int rolActual, int rolNuevo)
    {
        Actor(actor);
        Existe(Usuario(objetivo, rolActual));

        Es403(await sut.PutUsuarios(objetivo, EditarDto(rolNuevo)));
        NadaSeGuardo();
    }

    [Fact]
    public async Task PutUsuarios_SoporteEditaALaAdministradora_Retorna200()
    {
        Actor(soporte);
        Existe(Usuario(patricia, 1));

        (await sut.PutUsuarios(patricia, EditarDto(1))).Should().BeOfType<OkObjectResult>();
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
        sut.ModelState.AddModelError("nombres", "requerido");
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
        Existe(Usuario(5));
        repoMock.Setup(r => r.ExisteEmail("laura.m@elcuate.com", 5)).ReturnsAsync(true);

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ConflictObjectResult>();
        NadaSeGuardo();
    }

    [Fact]
    public async Task PutUsuarios_RepositorioNoActualiza_Retorna400()
    {
        Existe(Usuario(5));
        repoMock.Setup(r => r.ActualizarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ReturnsAsync(false);

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_ViolaIntegridad_Retorna409()
    {
        Existe(Usuario(5));
        repoMock.Setup(r => r.ActualizarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task PutUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("x"));
        (await sut.PutUsuarios(5, EditarDto())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // =====================================================================
    // CambiarPassword (solo la propia)
    // =====================================================================

    [Fact]
    public async Task CambiarPassword_Propia_ActualizaAuditaYEntregaTokenNuevo()
    {
        Existe(Usuario(patricia, 1));

        var resultado = await sut.CambiarPassword(patricia, Cambio());

        resultado.Should().BeOfType<OkObjectResult>();
        Respuestas.Leer(resultado, "token").Should().Be("jwt-nuevo");
        guardado!.passwordHash.Should().Be("HASH-NUEVO");
        auditoria!.accion.Should().Be("CAMBIAR_PASSWORD");
        auditoria.datosNuevos.Should().NotContain("ClaveNueva1").And.NotContain("HASH");
    }

    [Fact]
    public async Task CambiarPassword_DeOtroUsuario_Retorna403AunqueSeaAdministrador()
    {
        Es403(await sut.CambiarPassword(5, Cambio()));
        NadaSeGuardo();
    }

    [Fact]
    public async Task CambiarPassword_TemporalVigente_LaReemplazaYQuitaLaMarca()
    {
        var u = Usuario(5);
        u.debeCambiarPassword = true;
        u.passwordTemporalExpira = DateTime.UtcNow.AddHours(3);
        Actor(5);
        Existe(u);

        (await sut.CambiarPassword(5, Cambio("Temporal1234"))).Should().BeOfType<OkObjectResult>();
        guardado!.debeCambiarPassword.Should().BeFalse();
        guardado.passwordTemporalExpira.Should().BeNull();
        auditoria!.datosNuevos.Should().Contain("temporal");
    }

    [Fact]
    public async Task CambiarPassword_TemporalVencida_Retorna400SinCambiar()
    {
        var u = Usuario(5);
        u.debeCambiarPassword = true;
        u.passwordTemporalExpira = DateTime.UtcNow.AddMinutes(-1);
        Actor(5);
        Existe(u);

        (await sut.CambiarPassword(5, Cambio("Temporal1234"))).Should().BeOfType<BadRequestObjectResult>();
        NadaSeGuardo();
    }

    [Fact]
    public async Task CambiarPassword_ContrasenaActualIncorrecta_Retorna400SinCambiar()
    {
        Existe(Usuario(patricia, 1));
        ContrasenaEs(PasswordVerificationResult.Failed);

        (await sut.CambiarPassword(patricia, Cambio())).Should().BeOfType<BadRequestObjectResult>();
        NadaSeGuardo();
    }

    [Fact]
    public async Task CambiarPassword_ConfirmacionNoCoincide_Retorna400()
    {
        Existe(Usuario(patricia, 1));
        (await sut.CambiarPassword(patricia, Cambio(confirmar: "Otra12345"))).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_NuevaIgualALaActual_Retorna400()
    {
        Existe(Usuario(patricia, 1));
        (await sut.CambiarPassword(patricia, Cambio("Misma12345", "Misma12345"))).Should().BeOfType<BadRequestObjectResult>();
        NadaSeGuardo();
    }

    [Fact]
    public async Task CambiarPassword_UsuarioNoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(patricia)).ReturnsAsync((Usuarios?)null);
        (await sut.CambiarPassword(patricia, Cambio())).Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task CambiarPassword_IdNoPositivo_Retorna400(int id)
    {
        (await sut.CambiarPassword(id, Cambio())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_ModeloInvalido_Retorna400()
    {
        sut.ModelState.AddModelError("nuevaPassword", "corta");
        (await sut.CambiarPassword(patricia, Cambio())).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CambiarPassword_RepositorioNoActualiza_Retorna400()
    {
        Existe(Usuario(patricia, 1));
        repoMock.Setup(r => r.ActualizarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ReturnsAsync(false);

        (await sut.CambiarPassword(patricia, Cambio())).Should().BeOfType<BadRequestObjectResult>();
        tokenMock.Verify(t => t.GenerarToken(It.IsAny<Usuarios>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPassword_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(patricia)).ThrowsAsync(new Exception("x"));
        (await sut.CambiarPassword(patricia, Cambio())).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // =====================================================================
    // RestablecerPassword
    // =====================================================================

    [Fact]
    public async Task RestablecerPassword_AdministradoraAEmpleado_GeneraTemporalQueVenceEn24Horas()
    {
        Existe(Usuario(5));

        var resultado = await sut.RestablecerPassword(5);

        resultado.Should().BeOfType<OkObjectResult>();
        var temporal = (string)Respuestas.Leer(resultado, "passwordTemporal")!;
        temporal.Should().HaveLength(12);
        hasherMock.Verify(h => h.HashPassword(It.IsAny<Usuarios>(), temporal), Times.Once);
        guardado!.passwordHash.Should().Be("HASH-NUEVO");
        guardado.debeCambiarPassword.Should().BeTrue();
        guardado.passwordTemporalExpira.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromSeconds(10));
        auditoria!.accion.Should().Be("RESTABLECER_PASSWORD");
        auditoria.idUsuario.Should().Be(patricia);
        auditoria.datosNuevos.Should().NotContain(temporal);
    }

    [Theory]
    [InlineData(otroAdmin, 5)]       // solo Soporte y la administradora
    [InlineData(patricia, soporte)]  // la de Soporte no se restablece
    [InlineData(patricia, patricia)] // la propia se cambia con la actual
    [InlineData(otroAdmin, patricia)]
    public async Task RestablecerPassword_ReglaIncumplida_Retorna403SinCambiar(int actor, int objetivo)
    {
        Actor(actor);

        Es403(await sut.RestablecerPassword(objetivo));
        NadaSeGuardo();
    }

    [Fact]
    public async Task RestablecerPassword_SoporteALaAdministradora_Retorna200()
    {
        Actor(soporte);
        Existe(Usuario(patricia, 1));

        (await sut.RestablecerPassword(patricia)).Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RestablecerPassword_NoExiste_Retorna404()
    {
        repoMock.Setup(r => r.GetUsuariosById(99)).ReturnsAsync((Usuarios?)null);
        (await sut.RestablecerPassword(99)).Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public async Task RestablecerPassword_IdNoPositivo_Retorna400(int id)
    {
        (await sut.RestablecerPassword(id)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RestablecerPassword_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("x"));
        (await sut.RestablecerPassword(5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // =====================================================================
    // DeleteUsuarios
    // =====================================================================

    [Fact]
    public async Task DeleteUsuarios_AdministradoraEliminaEmpleado_EliminaConAuditoria()
    {
        Existe(Usuario(5));

        (await sut.DeleteUsuarios(5)).Should().BeOfType<OkObjectResult>();
        auditoria!.accion.Should().Be("DELETE");
        auditoria.datosAnteriores.Should().Contain("u5@elcuate.com");
        auditoria.datosNuevos.Should().BeNull();
    }

    [Theory]
    [InlineData(otroAdmin, 5)]        // solo Soporte y la administradora eliminan
    [InlineData(patricia, soporte)]   // Soporte no se elimina
    [InlineData(patricia, patricia)]  // nadie se elimina a sí mismo
    public async Task DeleteUsuarios_ReglaIncumplida_Retorna403SinEliminar(int actor, int objetivo)
    {
        Actor(actor);

        Es403(await sut.DeleteUsuarios(objetivo));
        NadaSeGuardo();
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
        Existe(Usuario(5));
        repoMock.Setup(r => r.EliminarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ReturnsAsync(false);

        (await sut.DeleteUsuarios(5)).Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_TieneRegistrosAsociados_Retorna409()
    {
        Existe(Usuario(5));
        repoMock.Setup(r => r.EliminarConAuditoria(It.IsAny<Usuarios>(), It.IsAny<Auditorias>())).ThrowsAsync(new DbUpdateException("FK"));

        (await sut.DeleteUsuarios(5)).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteUsuarios_ErrorInesperado_Retorna500()
    {
        repoMock.Setup(r => r.GetUsuariosById(5)).ThrowsAsync(new Exception("x"));
        (await sut.DeleteUsuarios(5)).Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
