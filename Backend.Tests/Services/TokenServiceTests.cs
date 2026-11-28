using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElQuateDePatty.Models;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>TokenService: el JWT lleva los datos correctos, vence a tiempo y está firmado con la clave de la API.</summary>
public class TokenServiceTests
{
    private static readonly JwtSettings ajustes = new()
    {
        key = "clave-de-pruebas-unitarias-de-mas-de-32-bytes",
        issuer = "ElQuateDePatty",
        audience = "ElQuateDePatty",
        expirationMinutes = 30
    };

    private static readonly Usuarios laura = new() { idUsuario = 5, idRol = 2, email = "laura@elcuate.com", passwordHash = "HASH-LAURA" };

    private static readonly JwtSettings ajustes60 = new()
    {
        key = "clave-de-pruebas-unitarias-de-mas-de-32-bytes",
        issuer = "ElQuateDePatty",
        audience = "ElQuateDePatty",
        expirationMinutes = 60,
        sesionMaximaHoras = 14
    };

    private static ClaimsPrincipal Validar(string token, string clave)
    {
        var parametros = new TokenValidationParameters
        {
            ValidIssuer = "ElQuateDePatty",
            ValidAudience = "ElQuateDePatty",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
        return new JwtSecurityTokenHandler().ValidateToken(token, parametros, out _);
    }

    [Fact]
    public void GenerarToken_UsuarioValido_LlevaIdRolCorreoSesionYSello()
    {
        var resultado = new TokenService(ajustes).GenerarToken(laura);

        var principal = Validar(resultado.token, ajustes.key);

        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("5");
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("2");
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be("laura@elcuate.com");
        principal.FindFirst(SelloSeguridad.claimSesion)!.Value.Should().NotBeNullOrEmpty();
        principal.FindFirst(SelloSeguridad.claimSello)!.Value.Should().Be(SelloSeguridad.Calcular("HASH-LAURA"));
    }

    [Fact]
    public void GenerarToken_NoIncluyeElHashDeLaContrasena()
    {
        var token = new TokenService(ajustes).GenerarToken(laura).token;

        // El JWT se puede decodificar sin la clave: nunca debe llevar el hash
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Select(c => c.Value).Should().NotContain("HASH-LAURA");
    }

    [Fact]
    public void GenerarToken_ExpiraALos30Minutos()
    {
        var antes = DateTime.UtcNow;

        var resultado = new TokenService(ajustes).GenerarToken(laura);

        resultado.expiraEn.Should().BeCloseTo(antes.AddMinutes(30), TimeSpan.FromSeconds(5));
        new JwtSecurityTokenHandler().ReadJwtToken(resultado.token).ValidTo.Should().BeCloseTo(antes.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerarToken_UsaHmacSha256()
    {
        var token = new TokenService(ajustes).GenerarToken(laura).token;

        new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GenerarToken_ConOtraClave_NoPasaLaValidacion()
    {
        var token = new TokenService(ajustes).GenerarToken(laura).token;

        var accion = () => Validar(token, "otra-clave-diferente-de-mas-de-32-bytes!!");

        accion.Should().Throw<SecurityTokenException>();   // firma inválida
    }

    [Fact]
    public void GenerarToken_DosVeces_GeneraSesionesDistintas()
    {
        var servicio = new TokenService(ajustes);

        var s1 = Validar(servicio.GenerarToken(laura).token, ajustes.key).FindFirst(SelloSeguridad.claimSesion)!.Value;
        var s2 = Validar(servicio.GenerarToken(laura).token, ajustes.key).FindFirst(SelloSeguridad.claimSesion)!.Value;

        s1.Should().NotBe(s2);   // así un logout solo revoca esa sesión
    }

    // ---------- Sesión renovable ----------

    [Fact]
    public void GenerarToken_LlevaElInicioDeSesion()
    {
        var antes = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var principal = Validar(new TokenService(ajustes60).GenerarToken(laura).token, ajustes60.key);

        long.Parse(principal.FindFirst(SelloSeguridad.claimInicio)!.Value).Should().BeCloseTo(antes, 5);
    }

    [Fact]
    public void RenovarToken_ConservaLaSesionYElInicioYDura60Minutos()
    {
        var inicio = DateTime.UtcNow.AddHours(-2);

        var resultado = new TokenService(ajustes60).RenovarToken(laura, "sesion-1", inicio)!;
        var principal = Validar(resultado.token, ajustes60.key);

        principal.FindFirst(SelloSeguridad.claimSesion)!.Value.Should().Be("sesion-1");
        principal.FindFirst(SelloSeguridad.claimInicio)!.Value.Should().Be(new DateTimeOffset(inicio).ToUnixTimeSeconds().ToString());
        resultado.expiraEn.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RenovarToken_SesionQueSuperoLas14Horas_RetornaNull()
    {
        new TokenService(ajustes60).RenovarToken(laura, "sesion-1", DateTime.UtcNow.AddHours(-14).AddSeconds(-1))
            .Should().BeNull();
    }

    [Fact]
    public void RenovarToken_CercaDelTope_NoVencePasadasLas14Horas()
    {
        var inicio = DateTime.UtcNow.AddHours(-14).AddMinutes(20);

        var resultado = new TokenService(ajustes60).RenovarToken(laura, "sesion-1", inicio)!;

        resultado.expiraEn.Should().BeCloseTo(inicio.AddHours(14), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GenerarToken_ConContrasenaTemporal_LlevaLaMarcaDeCambio()
    {
        var conTemporal = new Usuarios { idUsuario = 6, idRol = 2, email = "temp@elcuate.com", passwordHash = "HASH-T", debeCambiarPassword = true };

        var principal = Validar(new TokenService(ajustes60).GenerarToken(conTemporal).token, ajustes60.key);

        principal.FindFirst(SelloSeguridad.claimCambioPendiente)!.Value.Should().Be("true");
    }

    [Fact]
    public void GenerarToken_SinContrasenaTemporal_NoLlevaLaMarca()
    {
        var principal = Validar(new TokenService(ajustes60).GenerarToken(laura).token, ajustes60.key);

        principal.FindFirst(SelloSeguridad.claimCambioPendiente).Should().BeNull();
    }
}
