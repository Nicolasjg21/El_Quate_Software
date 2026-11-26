using ElQuateDePatty.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Tabla completa de quién puede administrar a quién (bloque 2.12).
/// Soporte = 15, administradora principal = 1, otro administrador = 7, empleados = 20 y 21.
/// </summary>
public class ReglasUsuariosTests
{
    private const int soporte = 15, patricia = 1, otroAdmin = 7, empleado = 20, otroEmpleado = 21;

    private static readonly ReglasUsuarios reglas = new(new UsuariosProtegidosSettings { idSoporte = soporte, idAdministradora = patricia });

    // ---------- Crear ----------

    [Theory]
    [InlineData(soporte, false, true)]
    [InlineData(soporte, true, true)]
    [InlineData(patricia, true, true)]
    [InlineData(otroAdmin, false, true)]
    [InlineData(otroAdmin, true, false)]    // un administrador cualquiera no crea administradores
    public void Crear(int actor, bool rolAdministrador, bool permitido)
    {
        (reglas.Crear(actor, rolAdministrador) == null).Should().Be(permitido);
    }

    // ---------- Editar: (actor, objetivo, objetivoEsAdmin, rolNuevoEsAdmin, permitido) ----------

    [Theory]
    // Soporte no lo edita nadie, ni él mismo
    [InlineData(soporte, soporte, true, true, false)]
    [InlineData(patricia, soporte, true, true, false)]
    [InlineData(otroAdmin, soporte, true, true, false)]
    // A la administradora principal solo la edita Soporte
    [InlineData(soporte, patricia, true, true, true)]
    [InlineData(patricia, patricia, true, true, false)]
    [InlineData(otroAdmin, patricia, true, true, false)]
    // A otro administrador solo lo editan Soporte y la administradora
    [InlineData(soporte, otroAdmin, true, true, true)]
    [InlineData(patricia, otroAdmin, true, true, true)]
    [InlineData(patricia, otroAdmin, true, false, true)]    // quitarle el rol de administrador
    [InlineData(otroEmpleado, otroAdmin, true, true, false)]
    [InlineData(otroAdmin, otroAdmin, true, true, false)]    // nadie se edita a sí mismo
    // A un empleado lo edita cualquier administrador, pero solo los dos superiores lo vuelven administrador
    [InlineData(otroAdmin, empleado, false, false, true)]
    [InlineData(otroAdmin, empleado, false, true, false)]
    [InlineData(patricia, empleado, false, true, true)]
    [InlineData(soporte, empleado, false, true, true)]
    public void Editar(int actor, int objetivo, bool objetivoEsAdmin, bool rolNuevoEsAdmin, bool permitido)
    {
        (reglas.Editar(actor, objetivo, objetivoEsAdmin, rolNuevoEsAdmin) == null).Should().Be(permitido);
    }

    // ---------- Eliminar ----------

    [Theory]
    [InlineData(soporte, empleado, true)]
    [InlineData(patricia, empleado, true)]
    [InlineData(soporte, patricia, true)]
    [InlineData(soporte, otroAdmin, true)]
    [InlineData(patricia, otroAdmin, true)]
    [InlineData(otroAdmin, empleado, false)]    // solo Soporte y la administradora eliminan
    [InlineData(patricia, soporte, false)]      // Soporte no se elimina
    [InlineData(soporte, soporte, false)]
    [InlineData(patricia, patricia, false)]     // nadie se elimina a sí mismo
    [InlineData(otroAdmin, patricia, false)]
    public void Eliminar(int actor, int objetivo, bool permitido)
    {
        (reglas.Eliminar(actor, objetivo) == null).Should().Be(permitido);
    }

    // ---------- Restablecer contraseña ----------

    [Theory]
    [InlineData(soporte, empleado, true)]
    [InlineData(patricia, empleado, true)]
    [InlineData(soporte, patricia, true)]
    [InlineData(patricia, otroAdmin, true)]
    [InlineData(otroAdmin, empleado, false)]
    [InlineData(patricia, soporte, false)]
    [InlineData(soporte, soporte, false)]       // la propia se cambia con la actual
    [InlineData(patricia, patricia, false)]
    [InlineData(otroAdmin, patricia, false)]
    public void Restablecer(int actor, int objetivo, bool permitido)
    {
        (reglas.Restablecer(actor, objetivo) == null).Should().Be(permitido);
    }

    // ---------- Perfil propio (teléfono) ----------

    [Theory]
    [InlineData(soporte, false)]
    [InlineData(patricia, true)]
    [InlineData(otroAdmin, true)]
    [InlineData(empleado, true)]
    public void EditarPerfilPropio(int actor, bool permitido)
    {
        (reglas.EditarPerfilPropio(actor) == null).Should().Be(permitido);
    }

    [Fact]
    public void Rechazo_SiempreExplicaElMotivo()
    {
        reglas.Eliminar(otroAdmin, empleado).Should().Contain("Soporte técnico");
        reglas.Editar(otroAdmin, otroAdmin, true, true).Should().Contain("Configuración");
    }
}

/// <summary>Configuración de los usuarios protegidos y generador de claves temporales.</summary>
public class UsuariosProtegidosYClaveTemporalTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(15, 0)]
    [InlineData(-1, 1)]
    [InlineData(5, 5)]
    public void Settings_Invalidos_LanzanExcepcion(int idSoporte, int idAdministradora)
    {
        var s = new UsuariosProtegidosSettings { idSoporte = idSoporte, idAdministradora = idAdministradora };
        s.Invoking(x => x.Validar()).Should().Throw<InvalidOperationException>().WithMessage("*UsuariosProtegidos*");
    }

    [Fact]
    public void Settings_Validos_NoLanzan()
    {
        new UsuariosProtegidosSettings { idSoporte = 15, idAdministradora = 1 }.Invoking(x => x.Validar()).Should().NotThrow();
    }

    [Fact]
    public void ClaveTemporal_Tiene12CaracteresSinAmbiguos()
    {
        var clave = GeneradorPasswordTemporal.Generar();

        clave.Should().HaveLength(GeneradorPasswordTemporal.longitud);
        clave.Should().NotContainAny("0", "O", "1", "l", "I");
    }

    [Fact]
    public void ClaveTemporal_CadaVezEsDistinta()
    {
        var claves = Enumerable.Range(0, 200).Select(_ => GeneradorPasswordTemporal.Generar()).ToHashSet();

        claves.Should().HaveCount(200);
    }

    [Fact]
    public void ClaveTemporal_Vence24Horas()
    {
        GeneradorPasswordTemporal.vigencia.Should().Be(TimeSpan.FromHours(24));
    }
}
