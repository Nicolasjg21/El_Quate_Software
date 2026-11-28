using System.Security.Claims;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// La cadena de autorización por permisos:
/// [RequierePermiso("x")] → política "permiso:x" → PermisoPolicyProvider → PermisoRequirement → PermisoAuthorizationHandler.
/// </summary>
public class AutorizacionPorPermisosTests
{
    private static ClaimsPrincipal ConRol(string? rol) =>
        new(new ClaimsIdentity(rol is null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.Role, rol) }, "Prueba"));

    private static async Task<bool> Autoriza(string? rol, string permiso, Mock<IPermisosRolService> servicio, params string[] rolesAdmin)
    {
        var handler = new PermisoAuthorizationHandler(new AutorizacionSettings { rolesAdministrador = new HashSet<string>(rolesAdmin) }, servicio.Object);
        var requisito = new PermisoRequirement(permiso);
        var contexto = new AuthorizationHandlerContext(new[] { requisito }, ConRol(rol), null);

        await handler.HandleAsync(contexto);

        return contexto.HasSucceeded;
    }

    // ---------- PermisoAuthorizationHandler ----------

    [Fact]
    public async Task Handler_RolConElPermiso_Autoriza()
    {
        var servicio = new Mock<IPermisosRolService>();
        servicio.Setup(s => s.TienePermiso(2, "usuarios.gestionar")).ReturnsAsync(true);

        (await Autoriza("2", "usuarios.gestionar", servicio)).Should().BeTrue();
    }

    [Fact]
    public async Task Handler_RolSinElPermiso_NoAutoriza()
    {
        var servicio = new Mock<IPermisosRolService>();
        servicio.Setup(s => s.TienePermiso(2, It.IsAny<string>())).ReturnsAsync(false);

        (await Autoriza("2", "usuarios.gestionar", servicio)).Should().BeFalse();
    }

    [Fact]
    public async Task Handler_RolAdministradorDeConfiguracion_AutorizaSinConsultarPermisos()
    {
        var servicio = new Mock<IPermisosRolService>();

        (await Autoriza("1", "usuarios.gestionar", servicio, "1")).Should().BeTrue();
        servicio.Verify(s => s.TienePermiso(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("mesero")]   // un rol no numérico es inválido
    public async Task Handler_SinRolNumerico_NoAutorizaNiConsulta(string? rol)
    {
        var servicio = new Mock<IPermisosRolService>();

        (await Autoriza(rol, "usuarios.gestionar", servicio)).Should().BeFalse();
        servicio.Verify(s => s.TienePermiso(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // ---------- PermisoPolicyProvider ----------

    [Fact]
    public async Task PolicyProvider_NombreConPrefijo_CreaPoliticaConElPermisoYUsuarioAutenticado()
    {
        var proveedor = new PermisoPolicyProvider(Options.Create(new AuthorizationOptions()));

        var politica = await proveedor.GetPolicyAsync("permiso:kardex.modificar");

        politica.Should().NotBeNull();
        politica!.Requirements.OfType<PermisoRequirement>().Single().permiso.Should().Be("kardex.modificar");
        politica.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Should().ContainSingle();
    }

    [Fact]
    public async Task PolicyProvider_NombreSinPrefijo_DelegaEnElProveedorBase()
    {
        var proveedor = new PermisoPolicyProvider(Options.Create(new AuthorizationOptions()));

        (await proveedor.GetPolicyAsync("PoliticaQueNoExiste")).Should().BeNull();
    }

    [Fact]
    public async Task PolicyProvider_PrefijoEnMayusculas_NoSeConfundeConUnPermiso()
    {
        // StringComparison.Ordinal: "PERMISO:" no es el prefijo
        var proveedor = new PermisoPolicyProvider(Options.Create(new AuthorizationOptions()));

        (await proveedor.GetPolicyAsync("PERMISO:usuarios.gestionar")).Should().BeNull();
    }

    // ---------- Atributo, políticas y catálogo ----------

    [Fact]
    public void RequierePermiso_ArmaElNombreDeLaPolitica()
    {
        new RequierePermisoAttribute(PermisosSistema.usuariosGestionar).Policy.Should().Be("permiso:usuarios.gestionar");
    }

    [Fact]
    public void PoliticasAutorizacion_Permiso_AgregaElPrefijo()
    {
        PoliticasAutorizacion.Permiso("ventas.cobrar").Should().Be("permiso:ventas.cobrar");
    }

    [Fact]
    public void PermisoRequirement_GuardaElPermiso()
    {
        new PermisoRequirement("auditorias.consultar").permiso.Should().Be("auditorias.consultar");
    }

    [Fact]
    public void PermisosSistema_SonUnicosYConFormatoModuloAccion()
    {
        var permisos = typeof(PermisosSistema).GetFields()
            .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).ToList();

        permisos.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
        permisos.Should().AllSatisfy(p => p.Should().MatchRegex(@"^[a-z]+\.[a-z]+$"));
    }
}
