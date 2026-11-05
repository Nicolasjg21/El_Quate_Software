using Backend.Tests.Ayudas;
using ElQuateDePatty.Models;
using ElQuateDePatty.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Backend.Tests.Repositories;

/// <summary>Repositorios de seguridad: usuarios (correo único, sesión en caché) y roles-permisos (llave compuesta).</summary>
public class UsuariosYPermisosRepositoryTests
{
    private static UsuariosRepository Usuarios(ElQuateDePatty.Context.ElQuateDePattyContext ctx) =>
        new(ctx, new MemoryCache(new MemoryCacheOptions()));

    // ---------- UsuariosRepository ----------

    [Fact]
    public async Task GetUsuarios_DevuelveLosTres()
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).GetUsuarios()).Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUsuariosById_Existe_LoEncuentra()
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).GetUsuariosById(2))!.email.Should().Be("mesero@elcuate.com");
    }

    [Fact]
    public async Task GetUsuariosByEmail_Existe_LoEncuentra()
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).GetUsuariosByEmail("admin@elcuate.com"))!.idUsuario.Should().Be(1);
    }

    [Fact]
    public async Task GetUsuariosByEmail_NoExiste_DevuelveNull()
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).GetUsuariosByEmail("nadie@elcuate.com")).Should().BeNull();
    }

    [Theory]
    [InlineData("admin@elcuate.com", null, true)]    // al crear: el correo ya existe
    [InlineData("admin@elcuate.com", 1, false)]      // al editar al propio admin: su correo no cuenta como repetido
    [InlineData("admin@elcuate.com", 2, true)]       // al editar a otro usuario: sí está repetido
    [InlineData("nuevo@elcuate.com", null, false)]
    public async Task ExisteEmail_SegunElUsuarioExcluido_RespondeCorrectamente(string email, int? excluido, bool esperado)
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).ExisteEmail(email, excluido)).Should().Be(esperado);
    }

    [Fact]
    public async Task ObtenerUsuarioActivoParaSesion_Activo_DevuelveSoloLoNecesarioParaElToken()
    {
        using var ctx = EscenarioBar.Crear();

        var sesion = await Usuarios(ctx).ObtenerUsuarioActivoParaSesion(1);

        sesion.Should().NotBeNull();
        sesion!.idRol.Should().Be(1);
        sesion.passwordHash.Should().Be("HASH-1");
        sesion.email.Should().BeEmpty();   // la proyección no trae datos personales que no se necesitan
    }

    [Fact]
    public async Task ObtenerUsuarioActivoParaSesion_Inactivo_DevuelveNull()
    {
        using var ctx = EscenarioBar.Crear();
        (await Usuarios(ctx).ObtenerUsuarioActivoParaSesion(3)).Should().BeNull();
    }

    [Fact]
    public async Task PutUsuarios_DesactivarUsuario_InvalidaLaSesionEnCache()
    {
        using var ctx = EscenarioBar.Crear();
        var repo = Usuarios(ctx);
        (await repo.ObtenerUsuarioActivoParaSesion(2)).Should().NotBeNull();   // queda en caché

        var mesero = await repo.GetUsuariosById(2);
        mesero!.estado = false;
        await repo.PutUsuarios(mesero);

        // Sin esta invalidación, un usuario desactivado seguiría entrando hasta 30 s más
        (await repo.ObtenerUsuarioActivoParaSesion(2)).Should().BeNull();
    }

    [Fact]
    public async Task PostUsuarios_Nuevo_LoGuarda()
    {
        using var ctx = EscenarioBar.Crear();
        var nuevo = new Usuarios { nombres = "Sara", apellidos = "Ruiz", documento = "999", idTipoDocumento = 1, telefono = "300",
                                   passwordHash = "HASH", estado = true, idRol = 2, email = "sara@elcuate.com" };

        (await Usuarios(ctx).PostUsuarios(nuevo)).Should().BeTrue();
        (await ctx.usuarios.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task DeleteUsuarios_Existe_LoEliminaEInvalidaLaSesion()
    {
        using var ctx = EscenarioBar.Crear();
        var repo = Usuarios(ctx);
        await repo.ObtenerUsuarioActivoParaSesion(2);
        var mesero = await repo.GetUsuariosById(2);

        (await repo.DeleteUsuarios(mesero!)).Should().BeTrue();
        (await repo.ObtenerUsuarioActivoParaSesion(2)).Should().BeNull();
    }

    // ---------- RolesPermisosRepository ----------

    [Fact]
    public async Task GetRolesPermisos_DevuelveLasTresAsignaciones()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).GetRolesPermisos()).Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRolesPermisosById_LlaveCompuestaExiste_LaEncuentra()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).GetRolesPermisosById(1, 2)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetRolesPermisosById_ElMeseroNoTieneGestionarUsuarios_DevuelveNull()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).GetRolesPermisosById(2, 2)).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerNombresPermisosPorRol_Administrador_DevuelveSusDosPermisos()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).ObtenerNombresPermisosPorRol(1))
            .Should().BeEquivalentTo(new[] { "ventas.cobrar", "usuarios.gestionar" });
    }

    [Fact]
    public async Task ObtenerNombresPermisosPorRol_RolSinPermisos_DevuelveListaVacia()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).ObtenerNombresPermisosPorRol(99)).Should().BeEmpty();
    }

    [Fact]
    public async Task PostRolesPermisos_NuevaAsignacion_LaGuarda()
    {
        using var ctx = EscenarioBar.Crear();
        (await new RolesPermisosRepository(ctx).PostRolesPermisos(new RolesPermisos { idRol = 2, idPermiso = 2 })).Should().BeTrue();
        (await ctx.rolesPermisos.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task DeleteRolesPermisos_Existe_LaElimina()
    {
        using var ctx = EscenarioBar.Crear();
        var repo = new RolesPermisosRepository(ctx);
        var asignacion = await repo.GetRolesPermisosById(2, 1);

        (await repo.DeleteRolesPermisos(asignacion!)).Should().BeTrue();
        (await ctx.rolesPermisos.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task PutRolesPermisos_Existe_DevuelveTrue()
    {
        using var ctx = EscenarioBar.Crear();
        var repo = new RolesPermisosRepository(ctx);
        var asignacion = await repo.GetRolesPermisosById(1, 1);

        (await repo.PutRolesPermisos(asignacion!)).Should().BeTrue();
    }
}
