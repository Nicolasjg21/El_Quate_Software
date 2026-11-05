using System.Reflection;
using ElQuateDePatty.Controllers;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace Backend.Tests.Seguridad;

/// <summary>
/// Matriz de seguridad (caja blanca por inspección): qué protección declara cada controlador y acción.
/// Si alguien borra un [Authorize] o un [RequierePermiso] por error, estas pruebas se ponen en rojo.
/// </summary>
public class MatrizSeguridadControladoresTests
{
    private static readonly Type[] controladores = typeof(ProveedoresController).Assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ControllerBase))).ToArray();

    public static TheoryData<Type> Controladores()
    {
        var datos = new TheoryData<Type>();
        foreach (var c in controladores) datos.Add(c);
        return datos;
    }

    [Fact]
    public void Api_Tiene20Controladores()
    {
        controladores.Should().HaveCount(20);
    }

    [Theory]
    [MemberData(nameof(Controladores))]
    public void Controlador_EsApiController(Type controlador)
    {
        controlador.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Controladores))]
    public void Controlador_ExigeSesion_SalvoElAutenticador(Type controlador)
    {
        var exige = controlador.GetCustomAttribute<AuthorizeAttribute>() != null;

        exige.Should().Be(controlador != typeof(AutenticadorController));
    }

    [Fact]
    public void Autenticador_SoloElLoginEsAnonimoYElLogoutExigeSesion()
    {
        typeof(AutenticadorController).GetMethod("Login")!.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
        typeof(AutenticadorController).GetMethod("Logout")!.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }

    [Theory]
    [InlineData(typeof(AutenticadorController), "Login")]
    [InlineData(typeof(UsuariosController), "CambiarPassword")]
    public void AccionesConContrasena_TienenLimiteDeIntentos(Type controlador, string accion)
    {
        controlador.GetMethod(accion)!.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName.Should().Be("login");
    }

    [Theory]
    [InlineData(typeof(UsuariosController), "PostUsuarios", PermisosSistema.usuariosGestionar)]
    [InlineData(typeof(UsuariosController), "PutUsuarios", PermisosSistema.usuariosGestionar)]
    [InlineData(typeof(UsuariosController), "DeleteUsuarios", PermisosSistema.usuariosGestionar)]
    [InlineData(typeof(RolesController), "PostRoles", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(RolesController), "PutRoles", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(RolesController), "DeleteRoles", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(PermisosController), "PostPermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(PermisosController), "PutPermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(PermisosController), "DeletePermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(RolesPermisosController), "PostRolesPermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(RolesPermisosController), "PutRolesPermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(RolesPermisosController), "DeleteRolesPermisos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(KardexController), "PutKardex", PermisosSistema.kardexModificar)]
    [InlineData(typeof(KardexController), "DeleteKardex", PermisosSistema.kardexModificar)]
    [InlineData(typeof(AuditoriasController), "GetAuditorias", PermisosSistema.auditoriasConsultar)]
    [InlineData(typeof(AuditoriasController), "GetAuditoriaById", PermisosSistema.auditoriasConsultar)]
    [InlineData(typeof(AuditoriasController), "PutAuditoria", PermisosSistema.auditoriasModificar)]
    [InlineData(typeof(AuditoriasController), "DeleteAuditoria", PermisosSistema.auditoriasModificar)]
    public void AccionSensible_ExigeSuPermiso(Type controlador, string accion, string permiso)
    {
        var atributo = controlador.GetMethod(accion)!.GetCustomAttribute<RequierePermisoAttribute>();

        atributo.Should().NotBeNull($"{controlador.Name}.{accion} debe exigir el permiso {permiso}");
        atributo!.Policy.Should().Be(PoliticasAutorizacion.Permiso(permiso));
    }
}
