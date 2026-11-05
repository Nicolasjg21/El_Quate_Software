using ElQuateDePatty.Repositories.Interfaces;
using ElQuateDePatty.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>Autorización por permisos: qué puede hacer cada rol.</summary>
public class PermisosRolServiceTests
{
    private readonly Mock<IRolesPermisosRepository> repoMock = new();
    private readonly PermisosRolService sut;

    public PermisosRolServiceTests()
    {
        // Rol 2 = Mesero (ejemplo): puede tomar pedidos, no gestionar usuarios.
        repoMock.Setup(r => r.ObtenerNombresPermisosPorRol(2))
                .ReturnsAsync(new List<string> { "pedidos.gestionar", "mesas.consultar" });
        sut = new PermisosRolService(repoMock.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task TienePermiso_PermisoAsignado_RetornaTrue()
    {
        (await sut.TienePermiso(2, "pedidos.gestionar")).Should().BeTrue();
    }

    [Fact]
    public async Task TienePermiso_PermisoNoAsignado_RetornaFalse()
    {
        (await sut.TienePermiso(2, "usuarios.gestionar")).Should().BeFalse();
    }

    [Fact]
    public async Task TienePermiso_MayusculasDistintas_IgualLoReconoce()
    {
        (await sut.TienePermiso(2, "PEDIDOS.GESTIONAR")).Should().BeTrue();
    }

    [Fact]
    public async Task TienePermiso_VariasConsultas_ConsultaLaBaseUnaSolaVez()
    {
        // Caché de 60 s: evita ir a la base en cada petición.
        await sut.TienePermiso(2, "pedidos.gestionar");
        await sut.TienePermiso(2, "mesas.consultar");

        repoMock.Verify(r => r.ObtenerNombresPermisosPorRol(2), Times.Once);
    }
}
