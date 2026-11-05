using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ElQuateDePatty.Context;
using ElQuateDePatty.Models;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Pruebas de integración de la seguridad completa (JWT + OnTokenValidated + permisos), con la API en memoria.
/// Cada prueba crea sus propios usuarios con ids únicos, porque la base en memoria se comparte entre clases.
/// </summary>
public class PipelineSeguridadTests : IClassFixture<ApiFactory>
{
    private const string claveDePruebas = "clave-solo-para-pruebas-con-mas-de-32-bytes";   // la misma de ApiFactory
    private static int siguienteId = 70000 + Random.Shared.Next(0, 10000) * 10;

    private readonly ApiFactory factory;
    private readonly HttpClient client;

    public PipelineSeguridadTests(ApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    private static int NuevoId() => Interlocked.Increment(ref siguienteId);

    /// <summary>Crea un rol (opcionalmente con un permiso) y un usuario en la base en memoria.</summary>
    private Usuarios Sembrar(bool activo = true, string? permiso = null, string contrasena = "ClaveSegura1")
    {
        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ElQuateDePattyContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuarios>>();

        var idRol = NuevoId();
        ctx.roles.Add(new Roles { idRol = idRol, nombreRol = $"Rol {idRol}" });
        if (permiso != null)
        {
            var idPermiso = NuevoId();
            ctx.permisos.Add(new Permisos { idPermiso = idPermiso, nombrePermiso = permiso });
            ctx.rolesPermisos.Add(new RolesPermisos { idRol = idRol, idPermiso = idPermiso });
        }

        var id = NuevoId();
        var usuario = new Usuarios
        {
            idUsuario = id, nombres = "Prueba", apellidos = "Integración", documento = $"D{id}", idTipoDocumento = 1,
            telefono = "300", estado = activo, idRol = idRol, email = $"u{id}@pruebas.com"
        };
        usuario.passwordHash = hasher.HashPassword(usuario, contrasena);
        ctx.usuarios.Add(usuario);
        ctx.SaveChanges();
        return usuario;
    }

    private string Token(Usuarios usuario) =>
        factory.Services.GetRequiredService<ITokenService>().GenerarToken(usuario).token;

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, string? token, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        if (token != null) peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo != null) peticion.Content = JsonContent.Create(cuerpo);
        return client.SendAsync(peticion);
    }

    // ---------- Token válido ----------

    [Fact]
    public async Task TokenValido_UsuarioActivo_Entra()
    {
        var u = Sembrar();

        var r = await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", Token(u));

        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LoginReal_ConContrasenaCorrecta_EntregaUnTokenQueFunciona()
    {
        var u = Sembrar(contrasena: "ClaveSegura1");

        var login = await client.PostAsJsonAsync("/api/Autenticador/Login", new { email = u.email, password = "ClaveSegura1" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await login.Content.ReadFromJsonAsync<RespuestaLogin>())!.token;

        var r = await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", token);

        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record RespuestaLogin(string token);

    // ---------- Validaciones de OnTokenValidated ----------

    [Fact]
    public async Task TokenDeUsuarioInactivo_Retorna401()
    {
        var u = Sembrar(activo: false);

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", Token(u))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenConRolDistintoAlDeLaBase_Retorna401()
    {
        var u = Sembrar();
        var conOtroRol = new Usuarios { idUsuario = u.idUsuario, idRol = u.idRol + 1, email = u.email, passwordHash = u.passwordHash };

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", Token(conOtroRol))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenEmitidoAntesDeCambiarLaContrasena_Retorna401()
    {
        var u = Sembrar();
        var conContrasenaVieja = new Usuarios { idUsuario = u.idUsuario, idRol = u.idRol, email = u.email, passwordHash = "HASH-ANTERIOR" };

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", Token(conContrasenaVieja))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenDespuesDelLogout_Retorna401()
    {
        var u = Sembrar();
        var token = Token(u);

        (await Enviar(HttpMethod.Post, "/api/Autenticador/Logout", token)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", token)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- Validaciones del JWT ----------

    [Fact]
    public async Task TokenFirmadoConOtraClave_Retorna401()
    {
        var u = Sembrar();
        var falsificador = new TokenService(new JwtSettings
        {
            key = "clave-falsa-de-un-atacante-de-mas-de-32-bytes", issuer = "ElQuateDePatty", audience = "ElQuateDePatty", expirationMinutes = 30
        });

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", falsificador.GenerarToken(u).token)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenVencido_Retorna401()
    {
        var u = Sembrar();
        var jwt = new JwtSecurityToken(
            issuer: "ElQuateDePatty", audience: "ElQuateDePatty",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, u.idUsuario.ToString()), new Claim(ClaimTypes.Role, u.idRol.ToString()) },
            notBefore: DateTime.UtcNow.AddHours(-2), expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveDePruebas)), SecurityAlgorithms.HmacSha256));

        (await Enviar(HttpMethod.Get, $"/api/Usuarios/GetUsuariosById/{u.idUsuario}", new JwtSecurityTokenHandler().WriteToken(jwt))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("esto-no-es-un-token")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0.")]   // token sin firma (alg "none")
    public async Task TokenMalFormadoOSinFirma_Retorna401(string token)
    {
        (await Enviar(HttpMethod.Get, "/api/Proveedores/GetProveedores", token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- Permisos ----------

    [Fact]
    public async Task UsuarioSinPermiso_CrearUsuario_Retorna403()
    {
        var mesero = Sembrar();   // rol sin permisos

        (await Enviar(HttpMethod.Post, "/api/Usuarios/PostUsuarios", Token(mesero), new { })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UsuarioConPermiso_CrearUsuarioConDatosVacios_PasaLaAutorizacionYRetorna400()
    {
        var admin = Sembrar(permiso: PermisosSistema.usuariosGestionar);

        // 400 (validación) y no 403: demuestra que la autorización dejó pasar
        (await Enviar(HttpMethod.Post, "/api/Usuarios/PostUsuarios", Token(admin), new { })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UsuarioSinPermiso_ConsultarAuditorias_Retorna403()
    {
        var mesero = Sembrar();

        (await Enviar(HttpMethod.Get, "/api/Auditorias/GetAuditorias", Token(mesero))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
