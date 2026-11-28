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
        typeof(AutenticadorController).GetMethod("Renovar")!.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
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
    [InlineData(typeof(UsuariosController), "RestablecerPassword", PermisosSistema.usuariosGestionar)]
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
    [InlineData(typeof(CategoriasController), "PostCategoria", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(CategoriasController), "PutCategoria", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(CategoriasController), "DeleteCategoria", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProductosController), "PostProductos", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProductosController), "PutProductos", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProductosController), "DeleteProductos", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProveedoresController), "PostProveedores", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProveedoresController), "PutProveedores", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ProveedoresController), "DeleteProveedores", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ComprasController), "PostCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ComprasController), "PutCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ComprasController), "DeleteCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(DetalleComprasController), "PostDetalleCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(DetalleComprasController), "PutDetalleCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(DetalleComprasController), "DeleteDetalleCompra", PermisosSistema.inventarioGestionar)]
    [InlineData(typeof(ComprobantesController), "PostComprobante", PermisosSistema.cobrosGestionar)]
    [InlineData(typeof(ComprobantesController), "PutComprobante", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(ComprobantesController), "DeleteComprobante", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(MetodosPagoController), "PostMetodosPago", PermisosSistema.cobrosGestionar)]
    [InlineData(typeof(MetodosPagoController), "PutMetodosPago", PermisosSistema.cobrosGestionar)]
    [InlineData(typeof(MetodosPagoController), "DeleteMetodosPago", PermisosSistema.cobrosGestionar)]
    [InlineData(typeof(CuentasController), "Cobrar", PermisosSistema.cobrosGestionar)]
    [InlineData(typeof(CuentasController), "DeleteCuenta", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(PedidosController), "DeletePedidos", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(TipoDocumentoController), "PostTipoDocumento", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(TipoDocumentoController), "PutTipoDocumento", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(TipoDocumentoController), "DeleteTipoDocumento", PermisosSistema.seguridadGestionar)]
    [InlineData(typeof(MesasController), "PostMesa", PermisosSistema.mesasGestionar)]
    [InlineData(typeof(MesasController), "DeleteMesa", PermisosSistema.mesasGestionar)]
    public void AccionSensible_ExigeSuPermiso(Type controlador, string accion, string permiso)
    {
        var atributo = controlador.GetMethod(accion)!.GetCustomAttribute<RequierePermisoAttribute>();

        atributo.Should().NotBeNull($"{controlador.Name}.{accion} debe exigir el permiso {permiso}");
        atributo!.Policy.Should().Be(PoliticasAutorizacion.Permiso(permiso));
    }

    /// <summary>
    /// La atención del salón queda abierta a cualquier rol con sesión: si alguien les pone un permiso
    /// por error, el mesero deja de poder tomar pedidos.
    /// </summary>
    [Theory]
    [InlineData(typeof(CuentasController), "PostCuenta")]
    [InlineData(typeof(CuentasController), "PutCuenta")]
    [InlineData(typeof(PedidosController), "PostPedidos")]
    [InlineData(typeof(PedidosController), "PutPedidos")]
    [InlineData(typeof(DetallePedidosController), "PostDetallePedido")]
    [InlineData(typeof(DetallePedidosController), "PutDetallePedido")]
    [InlineData(typeof(DetallePedidosController), "DeleteDetallePedido")]
    [InlineData(typeof(MesasController), "PutMesa")]
    [InlineData(typeof(KardexController), "PostKardex")]
    [InlineData(typeof(AuditoriasController), "PostAuditoria")]
    public void AccionDeAtencion_NoExigePermisoAdicional(Type controlador, string accion)
    {
        controlador.GetMethod(accion)!.GetCustomAttribute<RequierePermisoAttribute>().Should().BeNull();
    }

    /// <summary>
    /// Con una contraseña temporal pendiente solo se permiten estas cuatro acciones. Si alguien marca
    /// otra por error, un usuario con clave temporal podría operar sin haberla cambiado.
    /// </summary>
    [Fact]
    public void ContrasenaTemporal_SoloEstasAccionesSePermitenAntesDeCambiarla()
    {
        var permitidas = controladores
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<PermitidoConCambioPendienteAttribute>() != null)
                .Select(m => $"{c.Name}.{m.Name}"))
            .OrderBy(x => x);

        permitidas.Should().Equal(
            "AuditoriasController.PostAuditoria",
            "AutenticadorController.Logout",
            "AutenticadorController.Renovar",
            "UsuariosController.CambiarPassword");
    }

    [Theory]
    [InlineData("GetMiPerfil")]
    [InlineData("PutMiPerfil")]
    public void MiPerfil_ExigeSesionPeroNoPermisoAdicional(string accion)
    {
        typeof(UsuariosController).GetMethod(accion)!.GetCustomAttribute<RequierePermisoAttribute>().Should().BeNull();
    }
}
